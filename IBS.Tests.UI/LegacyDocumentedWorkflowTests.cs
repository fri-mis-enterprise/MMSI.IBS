using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class LegacyDocumentedWorkflowTests : PlaywrightTestBase
    {
        public LegacyDocumentedWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Legacy_JobOrder_Workflow()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");

            await SelectModernOptionAsync("Port", "SUBIC");
            await SelectModernOptionAsync("Terminal", "COASTAL");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");

            await Page.FillAsync("#PlannedStartTime", "2026-06-10T08:00");
            await Page.FillAsync("#PlannedEndTime", "2026-06-10T12:00");

            await Page.ClickAsync("#jobOrderForm button[type='submit']");
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));

            var joNumberText = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            var joNumber = joNumberText.Split('#').Last().Trim();

            // 2. Create Dispatch Ticket from JO
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var ticketNo = $"T-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#DispatchNumber", ticketNo);

            await SelectModernOptionAsync("Activity/Service Type", "TENDING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            // Fill Timeline
            await Page.FillAsync("#DateLeft", "2026-06-10");
            await Page.FillAsync("#TimeLeft", "08:00");
            await Page.FillAsync("#DateArrived", "2026-06-10");
            await Page.FillAsync("#TimeArrived", "10:00");

            await Page.ClickAsync("#submitButton");
            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            // 2.1 Set Tariff
            if (!Page.Url.Contains("/User/JobOrder/Details/"))
            {
                await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Index");
                await Page.FillAsync("input[type='search']", ticketNo);
                await Page.WaitForFunctionAsync(@"() => {
                    const rows = document.querySelectorAll('#paginatedTable tbody tr');
                    return rows.length >= 1 && !rows[0].innerText.includes('Loading');
                }");
            }

            var ticketRowInDetails = Page.Locator($"tr:has-text('{ticketNo}')");
            await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
            await ticketRowInDetails.Locator("button:has-text('Action')").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            await ForceClickAsync(ticketRowInDetails.Locator("text='Set Tariff'"));
            
            await Page.FillAsync("#DispatchRate", "35000");
            await Page.FillAsync("#BAFRate", "10000");
            
            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Submit");

            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            // 2.2 Approve Tariff
            if (!Page.Url.Contains("/User/JobOrder/Details/"))
            {
                await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Index");
                await Page.FillAsync("input[type='search']", joNumber);
                await Page.WaitForFunctionAsync(@"() => {
                    const rows = document.querySelectorAll('#jobOrdersTable tbody tr');
                    return rows.length === 1 && !rows[0].innerText.includes('Loading');
                }");
                await Page.ClickAsync("a:has-text('Details')");
                await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));
            }

            ticketRowInDetails = Page.Locator($"tr:has-text('{ticketNo}')");
            await ticketRowInDetails.ScrollIntoViewIfNeededAsync();
            await ticketRowInDetails.Locator("button:has-text('Action')").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            var approveBtn = ticketRowInDetails.Locator("button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
            await ForceClickAsync(approveBtn);
            await ConfirmSweetAlertAsync("approve");
            await ClickSweetAlertOkAsync();
            await DismissAnySweetAlertAsync();

            // 3. Create Billing for this JO
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");

            // Select the JO from the search results
            await Page.FillAsync("#JobOrderSearch", joNumber);
            await Page.WaitForSelectorAsync("#JobOrderSearchResults .modern-dropdown-item", new() { State = WaitForSelectorState.Attached });
            await ForceClickAsync(Page.Locator("#JobOrderSearchResults .modern-dropdown-item").Filter(new() { HasText = joNumber }));

            // REDUCED TO 10 CHARS MAX (DB Constraint)
            var billingNo = $"B-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);
            
            await Page.FillAsync("#VoyageNumber", "V-JO-101");
            await Page.FillAsync("#COSNumber", "COS-JO-101");

            // Ticket should be automatically selected when JO is selected
            await Page.WaitForFunctionAsync(@"() => {
                const total = document.querySelector('#OverallTotal');
                return total && total.innerText !== '0.00' && total.innerText !== '0';
            }");

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("submit");
            await ClickSweetAlertOkAsync();
            await Page.WaitForURLAsync($"{ServerAddress}/User/Billing");

            // 4. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

            var collectionNo = $"C-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#msapCollectionNumber", collectionNo);

            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

            var netAmountText = await Page.Locator("#netAmountDisplay").InnerTextAsync();
            var netAmount = decimal.Parse(netAmountText.Replace("₱", "").Replace(",", "").Trim());
            await Page.FillAsync("#cashAmount", netAmount.ToString());

            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Yes");
            await Page.WaitForURLAsync($"{ServerAddress}/User/Collection");

            Assert.Equal($"{ServerAddress}/User/Collection", Page.Url.TrimEnd('/'));
        }

        [Fact]
        public async Task Legacy_Documented_Workflow_Dec2025_Replication()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create a STANDALONE Dispatch Ticket (No Job Order)
            // This is required because Legacy Mode billing filters for JobOrderId == null
            await Page.GotoAsync($"{ServerAddress}/User/DispatchTicket/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var ticketNo = $"T-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#DispatchNumber", ticketNo);

            // Set Date to Dec 2025
            await Page.FillAsync("input[name='Date']", "2025-12-01");

            await SelectModernOptionAsync("Customer", "INSULAR OIL CORPORATION");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");
            await SelectModernOptionAsync("Activity/Service Type", "TENDING");
            await SelectModernOptionAsync("Port", "SUBIC");
            await SelectModernOptionAsync("Terminal", "COASTAL");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            await Page.FillAsync("#DateLeft", "2025-12-01");
            await Page.FillAsync("#TimeLeft", "08:00");
            await Page.FillAsync("#DateArrived", "2025-12-01");
            await Page.FillAsync("#TimeArrived", "10:00");

            await Page.ClickAsync("#submitButton");
            // Standalone ticket might redirect to Index or JO Details if auto-assigned (though unexpected)
            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            // 2. Set Tariff for replication ticket
            await Page.FillAsync("input[type='search']", ticketNo);
            await Page.WaitForTimeoutAsync(500); // Wait for search to filter
            var ticketRow = Page.Locator($"tr:has-text('{ticketNo}')");
            await ticketRow.Locator("button:has-text('Action')").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            await ForceClickAsync(ticketRow.Locator("text='Set Tariff'"));

            await Page.FillAsync("#DispatchRate", "25000");
            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Submit");
            await Page.WaitForURLAsync(new Regex("/User/(DispatchTicket($|/Index)|JobOrder/Details)"));
            await DismissAnySweetAlertAsync();

            // 3. Approve Tariff (Go via Preview or use dropdown if available)
            await Page.FillAsync("input[type='search']", ticketNo);
            await Page.WaitForTimeoutAsync(500);
            ticketRow = Page.Locator($"tr:has-text('{ticketNo}')");
            
            // Try to use dropdown first
            await ticketRow.Locator("button:has-text('Action')").ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            var approveBtnInDropdown = ticketRow.Locator("button.modern-dropdown-item:has-text('Approve Tariff')").Filter(new() { HasNotText = "Disapprove" });
            if (await approveBtnInDropdown.CountAsync() > 0 && await approveBtnInDropdown.IsVisibleAsync())
            {
                await ForceClickAsync(approveBtnInDropdown);
            }
            else
            {
                // Fallback to Preview
                await Page.ClickAsync("a.modern-dropdown-item:has-text('View Details')");
                await Page.WaitForURLAsync(new Regex("/User/DispatchTicket/Preview"));
                await Page.ClickAsync("#approveTariff");
            }

            await ConfirmSweetAlertAsync("approve");
            await DismissAnySweetAlertAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 4. Create Billing (Legacy Mode)
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            // Select Customer
            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");

            // Enable Legacy Mode (No JO)
            await Page.ClickAsync("label[for='legacyToggle']");
            await Page.WaitForSelectorAsync("#legacyNotice", new() { State = WaitForSelectorState.Visible });

            // Set Date to Dec 2025
            await Page.FillAsync("input[name='Date']", "2025-12-02");

            await Page.FillAsync("#VoyageNumber", "V-LEGACY-1224");

            // Select Port, Terminal and Vessel
            await SelectModernOptionAsync("Port", "SUBIC");
            await SelectModernOptionAsync("Terminal", "COASTAL");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");

            // FILL BILLING NUMBER - complies with 10 char limit
            var billingNo = $"L-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);

            // Wait for tickets to load
            await Page.WaitForFunctionAsync(@"() => {
                const rows = document.querySelectorAll('#ticketsTable tbody tr');
                return rows.length > 0 && !rows[0].innerText.includes('Loading');
            }", arg: null, options: new() { Timeout = 20000 });

            var ticketCheckboxes = Page.Locator("#ticketsTable tbody tr .ticket-checkbox");
            var selectAll = Page.Locator("#selectAllTickets");
            await selectAll.SetCheckedAsync(false);
            await ticketCheckboxes.First.SetCheckedAsync(true);

            await Page.WaitForFunctionAsync(@"() => {
                const total = document.querySelector('#OverallTotal');
                return total && total.innerText !== '0.00' && total.innerText !== '0';
            }");

            // Submit
            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("submit");
            await ClickSweetAlertOkAsync();
            await Page.WaitForURLAsync($"{ServerAddress}/User/Billing");

            // 2. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
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

            var collectionNo = $"C-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#msapCollectionNumber", collectionNo);

            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

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
