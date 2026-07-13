using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace IBS.Tests.UI
{
    public class EdgeCaseWorkflowTests : PlaywrightTestBase
    {
        public EdgeCaseWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Cannot_Close_JobOrder_With_Pending_Tickets()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await SelectModernOptionAsync("Customer", "INSULAR OIL CORPORATION");

            await SelectModernOptionAsync("Port", "SUBIC");
            await SelectModernOptionAsync("Terminal", "COASTAL");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");

            await Page.FillAsync("input[name='Date']", "2026-06-11");

            await Page.FillAsync("#PlannedStartTime", "2026-06-11T08:00");
            await Page.FillAsync("#PlannedEndTime", "2026-06-11T12:00");

            await Page.ClickAsync("button:has-text('Create Job Order')");
            await ConfirmSweetAlertAsync("Yes, create it!");
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));

            // 2. Add a ticket but don't set tariff (Status: For Tariff)
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            var ticketNo = $"T-EDGE-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#DispatchNumber", ticketNo);

            await SelectModernOptionAsync("Activity/Service Type", "TENDING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");

            await Page.FillAsync("#DateLeft", "2026-06-11");
            await Page.FillAsync("#TimeLeft", "08:00");
            await Page.FillAsync("#DateArrived", "2026-06-11");
            await Page.FillAsync("#TimeArrived", "10:00");

            await Page.ClickAsync("#submitButton");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            // 3. Attempt to close Job Order
            await Page.ClickAsync("button:has-text('Close Order')");
            await ConfirmSweetAlertAsync("close it");

            // 4. Verify error message
            var swalText = await Page.Locator(".swal2-html-container").InnerTextAsync();
            Assert.Contains("Cannot close Job Order", swalText);
            Assert.Contains("ticket(s) are in non-terminal states", swalText);

            await ClickSweetAlertOkAsync();
        }

        [Fact]
        public async Task Cannot_Create_Billing_Without_Tickets()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Job Order
            await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await SelectModernOptionAsync("Customer", "INSULAR OIL CORPORATION");

            await SelectModernOptionAsync("Port", "SUBIC");
            await SelectModernOptionAsync("Terminal", "COASTAL");
            await SelectModernOptionAsync("Vessel", "MT BULUSAN II");

            await Page.FillAsync("input[name='Date']", "2026-06-11");

            await Page.FillAsync("#VoyageNumber", "V-EDGE-101");
            await Page.FillAsync("#PlannedStartTime", "2026-06-11T14:00");
            await Page.FillAsync("#PlannedEndTime", "2026-06-11T16:00");

            await Page.ClickAsync("button:has-text('Create Job Order')");
            await ConfirmSweetAlertAsync("Yes, create it!");
            await Page.WaitForURLAsync(new Regex("/User/JobOrder/Details/\\d+"));

            var joNumberText = await Page.Locator("h1.modern-headline-lg").InnerTextAsync();
            var joNumber = joNumberText.Split('#').Last().Trim();

            // 2. Add a ticket and approve tariff so JO is billable/searchable
            await Page.ClickAsync("a:has-text('ADD TICKET')");
            await Page.FillAsync("#DispatchNumber", $"T-BILL-{Guid.NewGuid().ToString().Substring(0, 4)}");
            await SelectModernOptionAsync("Activity/Service Type", "TENDING");
            await SelectModernOptionAsync("Tugboat/Service provider", "AMATA MARU");
            await SelectModernOptionAsync("Master on Duty", "JOE MARIE J. TICAR");
            await Page.FillAsync("#DateLeft", "2026-06-11");
            await Page.FillAsync("#TimeLeft", "14:00");
            await Page.FillAsync("#DateArrived", "2026-06-11");
            await Page.FillAsync("#TimeArrived", "16:00");
            await Page.ClickAsync("#submitButton");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            var ticketRowInDetails = Page.Locator("tr").Filter(new() { HasText = "T-BILL" });
            await ticketRowInDetails.Locator("button").Filter(new() { HasText = "ACTIONS" }).ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            await ForceClickAsync(ticketRowInDetails.Locator("text='Set Tariff'"));

            await Page.WaitForURLAsync("**/User/DispatchTicket/SetTariff/*");
            await DismissAnySweetAlertAsync();

            await Page.FillAsync("#DispatchRate", "10000");
            await Page.ClickAsync("#submitButton");
            await ConfirmSweetAlertAsync("Submit");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            if (!Page.Url.Contains("/User/JobOrder/Details/"))
            {
                await Page.GotoAsync($"{ServerAddress}/User/JobOrder/Index");
                await Page.FillAsync("input[type='search']", joNumber);
                await Page.WaitForFunctionAsync(@"() => {
                    const rows = document.querySelectorAll('#jobOrdersTable tbody tr');
                    return rows.length === 1 && !rows[0].innerText.includes('Loading');
                }");
                await Page.ClickAsync("a:has-text('Details')");
                await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            }

            ticketRowInDetails = Page.Locator("tr").Filter(new() { HasText = "T-BILL" });
            await ticketRowInDetails.Locator("button").Filter(new() { HasText = "ACTIONS" }).ClickAsync();
            await Page.WaitForTimeoutAsync(300);
            
            var approveBtn = ticketRowInDetails.Locator(".modern-dropdown-item[onclick*='confirmApprove']");
            await ForceClickAsync(approveBtn);
            await ConfirmSweetAlertAsync("approve");
            await Page.WaitForURLAsync("**/User/JobOrder/Details/*");
            await DismissAnySweetAlertAsync();

            // 3. Go to Billing Create and select this JO
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await SelectModernOptionAsync("Customer", "INSULAR OIL CORPORATION");

            await Page.FillAsync("#JobOrderSearch", joNumber);
            // Use ForceClick for search results
            await ForceClickAsync(Page.Locator("#JobOrderSearchResults .modern-dropdown-item").Filter(new() { HasText = joNumber }));

            // 4. Deselect the ticket
            await Page.WaitForSelectorAsync(".ticket-checkbox");
            await Page.ClickAsync("#selectAllTickets"); // This should uncheck all since they are checked by default

            await Page.FillAsync("#billingNumber", $"FAIL-{Guid.NewGuid().ToString().Substring(0, 4)}");

            // 5. Attempt to submit
            await Page.ClickAsync("#submitButton");

            // 6. Verify SweetAlert error for no tickets
            var swalText = await Page.Locator(".swal2-html-container").InnerTextAsync();
            Assert.Contains("Please select at least one dispatch ticket", swalText);
        }
    }
}
