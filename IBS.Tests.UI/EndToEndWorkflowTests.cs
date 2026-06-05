using Microsoft.Playwright;
using Xunit;
using System.Text.RegularExpressions;

namespace IBS.Tests.UI
{
    public class EndToEndWorkflowTests : PlaywrightTestBase
    {
        public EndToEndWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task JobOrder_To_Collection_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync(".modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await SelectModernOptionAsync("Vessel", "BRP GREGORIO VELASQUEZ (LOCAL)");
            await SelectModernOptionAsync("Port", "BATANGAS");

            // Wait for Terminal options to load after Port selection
            await Page.WaitForFunctionAsync(@"() => {
                const select = document.querySelector('#TerminalId');
                return select && select.options.length > 1;
            }");

            await SelectModernOptionAsync("Terminal", "BBTI");

            await Page.ClickAsync("button:has-text('Create Job Order')");
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));

            Assert.Contains("/User/JobOrder/Details/", Page.Url);
            var jobOrderNumberText = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            var jobOrderNumber = jobOrderNumberText.Split('#').Last().Trim();

            // 2. Create Dispatch Ticket from JO
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var dispatchNo = $"DT-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("input[name='DispatchNumber']", dispatchNo);

            await SelectModernOptionAsync("Activity/Service Type", "DOCKING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            await Page.FillAsync("#TimeLeft", "10:00");
            await Page.FillAsync("#TimeArrived", "12:00");

            // Submit Ticket (Redirection happens automatically, no SweetAlert on Create)
            await Page.ClickAsync("#submitButton");
            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            if (Page.Url.Contains("/User/DispatchTicket/Create"))
            {
                var error = await Page.Locator(".text-error").InnerTextAsync();
                throw new Exception($"Ticket Creation Failed (Silent Error): {error}");
            }

            // 3. Set Tariff (Billing)
            await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Index");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("input[type='search']", dispatchNo);
            await Page.WaitForFunctionAsync(@"() => {
                const rows = document.querySelectorAll('#paginatedTable tbody tr');
                return rows.length >= 1 && !rows[0].innerText.includes('Loading');
            }");
            
            // Find the row for this dispatch ticket
            var row = Page.Locator($"tr:has-text('{dispatchNo}')");
            
            // Click the 'Action' button to show the dropdown
            await row.Locator("button:has-text('Action')").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            // Now click 'Set Tariff'
            await ForceClickAsync(row.Locator("text='Set Tariff'"));

            await Page.WaitForSelectorAsync("button:has-text('SUBMIT TARIFF')");
            await Page.ClickAsync("button:has-text('SUBMIT TARIFF')");

            // Handle SweetAlert2 confirmation
            await ConfirmSweetAlertAsync("Submit");
            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            // 3.5 Approve Tariff (Required for Billing)
            if (!Page.Url.Contains("/User/JobOrder/Details/"))
            {
                await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Index");
                await Page.FillAsync("input[type='search']", jobOrderNumber);
                await Page.WaitForFunctionAsync(@"() => {
                    const rows = document.querySelectorAll('#jobOrdersTable tbody tr');
                    return rows.length >= 1 && !rows[0].innerText.includes('Loading');
                }");
                await Page.ClickAsync("a:has-text('Details')");
                await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));
            }
            
            // Find the ticket row and click Actions
            var ticketRowInDetails = Page.Locator($"tr:has-text('{dispatchNo}')");
            await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
            
            var actionsBtn = ticketRowInDetails.Locator("button:has-text('Action')");
            await actionsBtn.ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            // Click Approve Tariff - Fix strict mode violation
            var approveBtn = ticketRowInDetails.Locator(".modern-dropdown-menu button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
            await ForceClickAsync(approveBtn);
            
            // Handle SweetAlert2 confirmation
            await ConfirmSweetAlertAsync("approve");
            await ClickSweetAlertOkAsync();
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));
            await DismissAnySweetAlertAsync();

            // 4. Create Billing
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            // Select Job Order
            await Page.FillAsync("#JobOrderSearch", jobOrderNumber);
            
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await ForceClickAsync(Page.Locator("#JobOrderSearchResults .modern-dropdown-item").Filter(new() { HasText = jobOrderNumber }));
            }, r => r.Url.Contains("GetDispatchTicketsByJobOrder") && r.Status == 200);
            
            await Page.WaitForTimeoutAsync(1000); // Small buffer for DOM rendering

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
            await ConfirmSweetAlertAsync("submit");
            await ClickSweetAlertOkAsync();
            await Page.WaitForURLAsync($"{ServerAddress}/User/Billing");

            // 5. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1500); // Wait for DataTable to render

            // Select the billing row
            var billCheckbox = Page.Locator(".bill-checkbox").First;
            await billCheckbox.WaitForAsync();
            await billCheckbox.CheckAsync();

            await Page.WaitForTimeoutAsync(1500); // Wait for JS calculations to settle

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
            await ConfirmSweetAlertAsync("Yes");
            await Page.WaitForURLAsync($"{ServerAddress}/User/Collection");

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
