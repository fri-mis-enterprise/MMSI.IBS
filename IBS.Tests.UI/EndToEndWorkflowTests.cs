using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class EndToEndWorkflowTests : PlaywrightTestBase
    {
        public EndToEndWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        private async Task SelectModernOptionAsync(string label, string optionText)
        {
            // Map common labels to their container IDs for more reliable targeting
            var idMap = new Dictionary<string, string>
            {
                { "Vessel", "#VesselContainer" },
                { "Port", "#PortContainer" },
                { "Terminal", "#TerminalContainer" },
                { "Activity/Service Type", "#ServiceContainer" },
                { "Tugboat/Service provider", "#TugboatContainer" },
                { "Master on Duty", "#TugMasterContainer" },
                { "Customer", "#CustomerContainer" } // Assuming we might add this later
            };

            ILocator trigger;
            if (idMap.TryGetValue(label, out var id))
            {
                trigger = Page.Locator($"{id} .modern-select-trigger");
            }
            else
            {
                // Fallback to label-based search if ID is not mapped
                trigger = Page.Locator("div")
                    .Filter(new() { Has = Page.Locator($"label:has-text('{label}')") })
                    .Filter(new() { Has = Page.Locator(".modern-select-trigger") })
                    .First
                    .Locator(".modern-select-trigger");
            }

            await trigger.ScrollIntoViewIfNeededAsync();
            await trigger.ClickAsync();

            // The dropdown is appended to the body, so we look for the visible one
            var dropdown = Page.Locator(".modern-select-dropdown.show");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var searchInput = dropdown.Locator(".modern-select-search input");
            if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
            {
                await searchInput.FillAsync(optionText);
                await Page.WaitForTimeoutAsync(100);
            }

            var option = dropdown.Locator(".modern-select-option")
                .Filter(new() { HasText = optionText })
                .First;

            await option.ClickAsync();
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        [Fact]
        public async Task JobOrder_To_Collection_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync(".modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await SelectModernOptionAsync("Vessel", "BRP GREGORIO VELASQUEZ (LOCAL)");
            await SelectModernOptionAsync("Port", "BATANGAS");

            // Wait for Terminal options to load after Port selection
            var terminalTrigger = Page.Locator("div")
                .Filter(new() { Has = Page.Locator("label:has-text('Terminal')") })
                .Locator(".modern-select-trigger");
            
            // Wait for the terminal trigger text to NOT be the placeholder if it was selected, 
            // but here we are creating, so we just wait for the underlying select to have options.
            await Page.WaitForFunctionAsync(@"() => {
                const select = document.querySelector('#TerminalId');
                return select && select.options.length > 1;
            }");

            await SelectModernOptionAsync("Terminal", "BBTI");

            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.ClickAsync("button:has-text('Create Job Order')");
            });

            Assert.Contains("/User/JobOrder/Details/", Page.Url);
            var jobOrderNumber = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            jobOrderNumber = jobOrderNumber.Replace("Job Order #", "").Trim();

            // 2. Create Dispatch Ticket from JO
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.WaitForURLAsync("**/User/DispatchTicket/Create**");

            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var dispatchNo = $"DT-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("input[name='DispatchNumber']", dispatchNo);

            await SelectModernOptionAsync("Activity/Service Type", "DOCKING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            await Page.FillAsync("#TimeLeft", "10:00");
            await Page.FillAsync("#TimeArrived", "12:00");

            // Submit Ticket
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.ClickAsync("#submitButton");
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });

            if (Page.Url.Contains("/User/DispatchTicket/Create"))
            {
                var error = await Page.Locator(".text-error").InnerTextAsync();
                throw new Exception($"Ticket Creation Failed (Silent Error): {error}");
            }

            // 3. Set Tariff (Billing)
            await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Index");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("input[type='search']", dispatchNo);
            
            // Find the row for this dispatch ticket
            var row = Page.Locator($"tr:has-text('{dispatchNo}')");
            
            // Click the 'Action' button to show the dropdown
            await row.Locator("button:has-text('Action')").ClickAsync();
            
            // Now click 'Set Tariff'
            await Page.ClickAsync($"text='Set Tariff'");

            await Page.WaitForSelectorAsync("button:has-text('SUBMIT TARIFF')");
            await Page.ClickAsync("button:has-text('SUBMIT TARIFF')");

            // Handle SweetAlert2 confirmation
            var confirmButton = Page.Locator(".swal2-confirm:has-text('Yes, Submit')");
            await confirmButton.WaitForAsync();
            
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await confirmButton.ClickAsync();
            });

            // 3.5 Approve Tariff (Required for Billing)
            Assert.Contains("/User/JobOrder/Details/", Page.Url);
            
            // Find the ticket row and click Actions
            var ticketRowInDetails = Page.Locator($"tr:has-text('{dispatchNo}')");
            await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
            
            var actionsBtn = ticketRowInDetails.Locator(".dropdown-trigger");
            await actionsBtn.ClickAsync();
            
            // Click Approve Tariff
            var approveBtn = Page.Locator("button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
            await approveBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await approveBtn.ClickAsync();
            
            // Handle SweetAlert2 confirmation
            var approveConfirmButton = Page.Locator(".swal2-confirm:has-text('Yes, approve it!')");
            await approveConfirmButton.WaitForAsync();
            await approveConfirmButton.ClickAsync();
            
            // Wait for the "Approved!" success message and click OK
            var successOkButton = Page.Locator(".swal2-confirm:has-text('OK')");
            await successOkButton.WaitForAsync();
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await successOkButton.ClickAsync();
            });

            // 4. Create Billing
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            // Select Job Order
            await Page.FillAsync("#JobOrderSearch", jobOrderNumber);
            
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync($"#JobOrderSearchResults .modern-dropdown-item:has-text('{jobOrderNumber}')");
            }, r => r.Url.Contains("GetDispatchTicketsByJobOrder") && r.Status == 200);
            
            await Page.WaitForTimeoutAsync(500); // Small buffer for DOM rendering

            // Ensure required fields are filled
            await Page.FillAsync("#VoyageNumber", "V-123");
            await Page.FillAsync("#COSNumber", "COS-123");

            // Toggle Undocumented to avoid filling billing number manually
            await Page.ClickAsync("#undocBadge");
            await Page.WaitForTimeoutAsync(500);

            // Wait for tickets table to populate
            var ticketRow = Page.Locator($"#ticketsTable tr:has-text('{dispatchNo}')");
            await ticketRow.WaitForAsync();
            
            // Ensure checkbox is checked
            var billingCheckbox = ticketRow.Locator(".ticket-checkbox");
            if (!await billingCheckbox.IsCheckedAsync())
            {
                await billingCheckbox.CheckAsync();
            }

            // Submit Billing
            await Page.ClickAsync("#submitButton");

            // Handle SweetAlert2 confirmation (lowercase 's' in "submit")
            var billingConfirmButton = Page.Locator(".swal2-confirm:has-text('Yes, submit')");
            await billingConfirmButton.WaitForAsync();
            await billingConfirmButton.ClickAsync();
            
            // Handle SweetAlert2 "Success!" dialog after fetch completes
            var successBillingOkButton = Page.Locator(".swal2-confirm:has-text('OK')");
            await successBillingOkButton.WaitForAsync();
            
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await successBillingOkButton.ClickAsync();
            });

            // 5. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1000); // Wait for DataTable to render

            // Select the billing row
            var billCheckbox = Page.Locator(".bill-checkbox").First;
            await billCheckbox.WaitForAsync();
            await billCheckbox.CheckAsync();

            await Page.WaitForTimeoutAsync(1000); // Wait for JS calculations to settle

            // Toggle Undocumented
            await Page.ClickAsync("#undocBadge");
            await Page.WaitForTimeoutAsync(500);

            // Select Bank
            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

            // Fill Reference and Remarks
            await Page.FillAsync("input[name='ReferenceNo']", "REF-TEST-001");
            await Page.FillAsync("textarea[name='Remarks']", "Automated E2E Test Collection");

            // Submit Collection
            await Page.ClickAsync("#submitButton");

            // Handle SweetAlert2 confirmation
            var collectionConfirmButton = Page.Locator(".swal2-confirm:has-text('Yes,')");
            await collectionConfirmButton.WaitForAsync();
            
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await collectionConfirmButton.ClickAsync();
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });

            if (Page.Url.Contains("/User/Collection/Create"))
            {
                // Check for SweetAlert2 error
                var swal = Page.Locator(".swal2-html-container");
                if (await swal.IsVisibleAsync())
                {
                    var errorMsg = await swal.InnerTextAsync();
                    throw new Exception($"Collection Creation Failed (Swal): {errorMsg}");
                }

                var alerts = await Page.Locator(".alert, .validation-summary-errors").AllInnerTextsAsync();
                var fallbackMsg = string.Join(" | ", alerts.Select(a => a.Trim()).Where(t => !string.IsNullOrEmpty(t)));
                throw new Exception($"Collection Creation Failed: {fallbackMsg}");
            }

            Assert.Equal($"{ServerAddress}/User/Collection", Page.Url.TrimEnd('/'));
        }
    }
}
