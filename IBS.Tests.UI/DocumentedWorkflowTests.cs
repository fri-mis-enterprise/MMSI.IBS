using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class DocumentedWorkflowTests : PlaywrightTestBase
    {
        public DocumentedWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        private async Task SelectModernOptionAsync(string label, string optionText)
        {
            var idMap = new Dictionary<string, string>
            {
                { "Vessel", "#VesselContainer" },
                { "Port", "#PortContainer" },
                { "Terminal", "#TerminalContainer" },
                { "Activity/Service Type", "#ServiceContainer" },
                { "Tugboat/Service provider", "#TugboatContainer" },
                { "Master on Duty", "#TugMasterContainer" },
                { "Customer", "#CustomerContainer" },
                { "Deposit To Bank", "#BankContainer" }
            };

            ILocator trigger;
            if (idMap.TryGetValue(label, out var id))
            {
                trigger = Page.Locator($"{id} .modern-select-trigger");
            }
            else
            {
                trigger = Page.Locator("div")
                    .Filter(new() { Has = Page.Locator($"label:has-text('{label}')") })
                    .Filter(new() { Has = Page.Locator(".modern-select-trigger") })
                    .First
                    .Locator(".modern-select-trigger");
            }

            await trigger.ScrollIntoViewIfNeededAsync();
            await trigger.ClickAsync();

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
        public async Task MultiTicket_Documented_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync(".modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await SelectModernOptionAsync("Vessel", "BRP GREGORIO VELASQUEZ (LOCAL)");
            await SelectModernOptionAsync("Port", "BATANGAS");

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
            var jobOrderUrl = Page.Url;
            var jobOrderNumber = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            jobOrderNumber = jobOrderNumber.Replace("Job Order #", "").Trim();

            var dispatchTickets = new List<string>();
            int ticketCount = 5;

            // 2. Create 5 Dispatch Tickets
            for (int i = 1; i <= ticketCount; i++)
            {
                await Page.GotoAsync(jobOrderUrl);
                await Page.ClickAsync("a:has-text('ADD TICKET')");
                await Page.WaitForURLAsync("**/User/DispatchTicket/Create**");

                await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

                var dispatchNo = $"DT-{Guid.NewGuid().ToString().Substring(0, 8)}";
                dispatchTickets.Add(dispatchNo);
                await Page.FillAsync("input[name='DispatchNumber']", dispatchNo);

                await SelectModernOptionAsync("Activity/Service Type", "DOCKING");
                await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
                await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

                await Page.FillAsync("#TimeLeft", "10:00");
                await Page.FillAsync("#TimeArrived", "12:00");

                await Page.RunAndWaitForNavigationAsync(async () =>
                {
                    await Page.ClickAsync("#submitButton");
                }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });
            }

            // 3. Set and Approve Tariff for each Ticket
            foreach (var dt in dispatchTickets)
            {
                await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Index");
                await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

                await Page.FillAsync("input[type='search']", dt);
                var row = Page.Locator($"tr:has-text('{dt}')");
                await row.Locator("button:has-text('Action')").ClickAsync();
                await Page.ClickAsync($"text='Set Tariff'");

                await Page.WaitForSelectorAsync("button:has-text('SUBMIT TARIFF')");
                await Page.ClickAsync("button:has-text('SUBMIT TARIFF')");

                var confirmButton = Page.Locator(".swal2-confirm:has-text('Yes, Submit')");
                await confirmButton.WaitForAsync();
                await Page.RunAndWaitForNavigationAsync(async () =>
                {
                    await confirmButton.ClickAsync();
                });

                // Approve Tariff
                var ticketRowInDetails = Page.Locator($"tr:has-text('{dt}')");
                await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
                await ticketRowInDetails.Locator(".dropdown-trigger").ClickAsync();
                
                var approveBtn = Page.Locator("button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
                await approveBtn.WaitForAsync(new() { State = WaitForSelectorState.Visible });
                await approveBtn.ClickAsync();
                
                await Page.Locator(".swal2-confirm:has-text('Yes, approve it!')").ClickAsync();
                await Page.RunAndWaitForNavigationAsync(async () =>
                {
                    await Page.Locator(".swal2-confirm:has-text('OK')").ClickAsync();
                });
            }

            // 4. Create Documented Billing
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await Page.FillAsync("#JobOrderSearch", jobOrderNumber);
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync($"#JobOrderSearchResults .modern-dropdown-item:has-text('{jobOrderNumber}')");
            }, r => r.Url.Contains("GetDispatchTicketsByJobOrder") && r.Status == 200);

            await Page.FillAsync("#VoyageNumber", "V-MULT-123");
            await Page.FillAsync("#COSNumber", "COS-MULT-123");

            // FILL BILLING NUMBER (Documented)
            var billingNo = $"B-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);

            await Page.ClickAsync("#submitButton");
            await Page.Locator(".swal2-confirm:has-text('Yes, submit')").ClickAsync();
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.Locator(".swal2-confirm:has-text('OK')").ClickAsync();
            });

            // 5. Create Documented Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1000);

            // Use the new search bar to find our specific billing
            await Page.FillAsync("input[type='search']", billingNo);
            await Page.WaitForTimeoutAsync(500);

            // Find our billing row and click it (row click triggers selection in this UI)
            var billRow = Page.Locator("#billingsTable tbody tr").Filter(new() { HasText = billingNo });
            await billRow.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await billRow.ScrollIntoViewIfNeededAsync();
            await billRow.ClickAsync();

            // Wait for JS calculations to settle and verify selection
            await Page.WaitForFunctionAsync(@"() => {
                const netDisplay = document.querySelector('#netAmountDisplay');
                return netDisplay && netDisplay.innerText !== '₱ 0.00' && netDisplay.innerText !== '₱0.00';
            }");

            // FILL COLLECTION NUMBER (Documented)
            var collectionNo = $"OR-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#msapCollectionNumber", collectionNo);

            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

            await Page.FillAsync("input[name='ReferenceNo']", $"REF-{collectionNo}");
            await Page.FillAsync("textarea[name='Remarks']", "Documented Multi-Ticket E2E Test");

            // Fill payment amounts to match total
            var netAmountText = await Page.Locator("#netAmountDisplay").InnerTextAsync();
            var netAmount = decimal.Parse(netAmountText.Replace("₱", "").Replace(",", "").Trim());
            await Page.FillAsync("#cashAmount", netAmount.ToString());

            await Page.ClickAsync("#submitButton");
            await Page.Locator(".swal2-confirm:has-text('Yes,')").ClickAsync();
            
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                var successOk = Page.Locator(".swal2-confirm:has-text('OK')");
                await successOk.WaitForAsync();
                await successOk.ClickAsync();
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });

            Assert.Equal($"{ServerAddress}/User/Collection", Page.Url.TrimEnd('/'));
        }
    }
}
