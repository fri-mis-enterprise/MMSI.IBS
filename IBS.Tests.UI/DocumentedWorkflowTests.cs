using Microsoft.Playwright;
using Xunit;
using System.Text.RegularExpressions;

namespace IBS.Tests.UI
{
    public class DocumentedWorkflowTests : PlaywrightTestBase
    {
        public DocumentedWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task MultiTicket_Documented_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync(".modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await Page.FillAsync("input[name='Date']", "2026-06-06");

            await SelectModernOptionAsync("Vessel", "BRP GREGORIO VELASQUEZ (LOCAL)");
            await SelectModernOptionAsync("Port", "BATANGAS");

            await Page.WaitForFunctionAsync(@"() => {
                const select = document.querySelector('#TerminalId');
                return select && select.options.length > 1;
            }");

            await SelectModernOptionAsync("Terminal", "BBTI");

            await Page.FillAsync("#PlannedStartTime", "2026-06-06T08:00");
            await Page.FillAsync("#PlannedEndTime", "2026-06-06T20:00");

            await Page.ClickAsync("button:has-text('Create Job Order')");
            await ConfirmSweetAlertAsync("Yes, create it!");
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));

            Assert.Contains("/User/JobOrder/Details/", Page.Url);
            var jobOrderUrl = Page.Url;
            var jobOrderNumberText = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            var jobOrderNumber = jobOrderNumberText.Split('#').Last().Trim();

            var dispatchTickets = new List<string>();
            int ticketCount = 3; // Reduced for efficiency, but still "multi"

            // 2. Create Dispatch Tickets
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

                await Page.ClickAsync("#submitButton");
                await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
                await DismissAnySweetAlertAsync();
            }

            // 3. Set and Approve Tariff for each Ticket
            foreach (var dt in dispatchTickets)
            {
                await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Index");
                await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

                await Page.FillAsync("input[type='search']", dt);
                
                var ticketRow = Page.Locator("#paginatedTable tbody tr").Filter(new() { HasText = dt });
                await ticketRow.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 15000 });

                await ticketRow.Locator("button:has-text('Action')").ClickAsync();
                await Page.WaitForTimeoutAsync(300);
                await ForceClickAsync(ticketRow.Locator("text='Set Tariff'"));

                await Page.WaitForSelectorAsync("button:has-text('SUBMIT TARIFF')");
                await Page.ClickAsync("button:has-text('SUBMIT TARIFF')");

                await ConfirmSweetAlertAsync("Submit");
                await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
                await DismissAnySweetAlertAsync();

                // Approve Tariff
                if (!Page.Url.Contains("/User/JobOrder/Details/"))
                {
                    await Page.GotoAsync(jobOrderUrl);
                }

                var ticketRowInDetails = Page.Locator($"tr:has-text('{dt}')");
                await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
                await ticketRowInDetails.Locator("button:has-text('Action')").ClickAsync();
                await Page.WaitForTimeoutAsync(300);
                
                // Fix strict mode violation
                var approveBtn = ticketRowInDetails.Locator("button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
                await ForceClickAsync(approveBtn);
                
                await ConfirmSweetAlertAsync("approve");
                await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));
                await DismissAnySweetAlertAsync();
            }

            // 4. Create Documented Billing
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");

            await Page.FillAsync("#JobOrderSearch", jobOrderNumber);
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await ForceClickAsync(Page.Locator("#JobOrderSearchResults .modern-dropdown-item").Filter(new() { HasText = jobOrderNumber }));
            }, r => r.Url.Contains("GetDispatchTicketsByJobOrder") && r.Status == 200);

            await Page.FillAsync("#VoyageNumber", "V-MULT-123");
            await Page.FillAsync("#COSNumber", "COS-MULT-123");

            // FILL BILLING NUMBER (Documented)
            var billingNo = $"B-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("submit");
            await Page.WaitForURLAsync($"{ServerAddress}/User/Billing");
            await DismissAnySweetAlertAsync();

            // 5. Create Documented Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "FOUR DRAGONS");
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('FOUR DRAGONS SHIPPING SERVICES')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1500);

            // Use the new search bar to find our specific billing
            await Page.FillAsync("input[type='search']", billingNo);
            await Page.WaitForTimeoutAsync(500);

            // Find our billing row and click it
            var billRow = Page.Locator("#billingsTable tbody tr").Filter(new() { HasText = billingNo });
            await billRow.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await billRow.ScrollIntoViewIfNeededAsync();
            await billRow.ClickAsync();

            // Wait for JS calculations to settle
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

            var netAmountText = await Page.Locator("#netAmountDisplay").InnerTextAsync();
            var netAmount = decimal.Parse(netAmountText.Replace("₱", "").Replace(",", "").Trim());
            await Page.FillAsync("#cashAmount", netAmount.ToString());

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Yes");
            await Page.WaitForURLAsync($"{ServerAddress}/User/Collection");

            Assert.Equal($"{ServerAddress}/User/Collection", Page.Url.TrimEnd('/'));
        }
    }
}
