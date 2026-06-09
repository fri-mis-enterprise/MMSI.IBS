using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class UndocumentedWorkflowTests : PlaywrightTestBase
    {
        public UndocumentedWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Undocumented_Full_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync(".modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");
            await Page.FillAsync("input[name='Date']", "2026-06-10");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");
            await SelectModernOptionAsync("Port", "SUBIC");
            await Page.WaitForFunctionAsync(@"() => {
                const select = document.querySelector('#TerminalId');
                return select && select.options.length > 1;
            }");
            await SelectModernOptionAsync("Terminal", "COASTAL");

            await Page.FillAsync("#PlannedStartTime", "2026-06-10T10:00");
            await Page.FillAsync("#PlannedEndTime", "2026-06-10T12:00");

            await Page.ClickAsync("button:has-text('Create Job Order')");
            await ConfirmSweetAlertAsync("Yes, create it!");
            await Page.WaitForSelectorAsync("h1.modern-headline-lg:has-text('Job Order #')");

            var jobOrderNumberText = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            var jobOrderNumber = jobOrderNumberText.Split('#').Last().Trim();

            // 2. Create Dispatch Ticket from JO
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.WaitForURLAsync("**/User/DispatchTicket/Create**");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var ticketNo = $"T-INS-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#DispatchNumber", ticketNo);
            await SelectModernOptionAsync("Activity/Service Type", "TENDING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            await Page.FillAsync("#TimeLeft", "10:00");
            await Page.FillAsync("#TimeArrived", "12:00");

            await Page.ClickAsync("#submitButton");
            await Page.WaitForSelectorAsync("h1.modern-headline-lg:has-text('Job Order #')");
            await DismissAnySweetAlertAsync();

            // 3. Set Tariff
            var ticketRow = Page.Locator("tr").Filter(new() { HasText = ticketNo });
            await ticketRow.ScrollIntoViewIfNeededAsync();
            await ticketRow.Locator("button").Filter(new() { HasText = "ACTIONS" }).ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            await ForceClickAsync(ticketRow.Locator("text='Set Tariff'"));

            await Page.WaitForURLAsync("**/User/DispatchTicket/SetTariff/*");
            await DismissAnySweetAlertAsync();

            await Page.FillAsync("#DispatchRate", "25000");
            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Submit");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            // 4. Approve Tariff
            ticketRow = Page.Locator("tr").Filter(new() { HasText = ticketNo });
            await ticketRow.ScrollIntoViewIfNeededAsync();
            await ticketRow.Locator("button").Filter(new() { HasText = "ACTIONS" }).ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            var approveBtn = ticketRow.Locator(".modern-dropdown-item[onclick*='confirmApprove']");
            await ForceClickAsync(approveBtn);

            await ConfirmSweetAlertAsync("approve");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            // 5. Create Billing (Shorten Number for DB constraint)
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");

            await Page.FillAsync("#JobOrderSearch", jobOrderNumber);
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await ForceClickAsync(Page.Locator("#JobOrderSearchResults .modern-dropdown-item").Filter(new() { HasText = jobOrderNumber }));
            }, r => r.Url.Contains("GetDispatchTicketsByJobOrder") && r.Status == 200);

            await Page.FillAsync("input[name='Date']", "2026-06-11");
            await Page.FillAsync("#VoyageNumber", "V-INS-123");

            // FIXED: Use 9-character billing number to fit in character varying(10)
            var billingNo = $"BI{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);

            await Page.WaitForFunctionAsync(@"() => {
                const total = document.querySelector('#OverallTotal');
                return total && total.innerText !== '0.00' && total.innerText !== '0';
            }");

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("submit");
            await Page.WaitForURLAsync("**/User/Billing");
            await DismissAnySweetAlertAsync();

            // 6. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1500);

            await Page.FillAsync("input[type='search']", billingNo);
            await Page.WaitForTimeoutAsync(500);

            var billRow = Page.Locator("#billingsTable tbody tr").Filter(new() { HasText = billingNo });
            await billRow.ClickAsync();

            await Page.WaitForFunctionAsync(@"() => {
                const netDisplay = document.querySelector('#netAmountDisplay');
                return netDisplay && netDisplay.innerText !== '₱ 0.00' && netDisplay.innerText !== '₱0.00';
            }");

            var collectionNo = $"CI{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#msapCollectionNumber", collectionNo);
            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

            var netAmountText = await Page.Locator("#netAmountDisplay").InnerTextAsync();
            var netAmount = decimal.Parse(netAmountText.Replace("₱", "").Replace(",", "").Trim());
            
            await Page.FillAsync("#checkAmount", "0");
            await Page.FillAsync("#cashAmount", netAmount.ToString());

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Yes");
            await Page.WaitForURLAsync("**/User/Collection");

            Assert.Equal($"{ServerAddress}/User/Collection", Page.Url.TrimEnd('/'));
        }
    }
}
