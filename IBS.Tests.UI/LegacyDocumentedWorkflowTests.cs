using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class LegacyDocumentedWorkflowTests : PlaywrightTestBase
    {
        public LegacyDocumentedWorkflowTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        private async Task SelectModernOptionAsync(string label, string optionText)
        {
            var idMap = new Dictionary<string, string>
            {
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
        public async Task Legacy_Documented_Workflow_Dec2025_Replication()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // 1. Create Billing (Legacy Mode)
            await Page.GotoAsync($"{ServerAddress}/User/Billing/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            // Select Customer (INSULAR OIL CORPORATION matches RECID 3666 customer)
            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");

            // Enable Legacy Mode (No JO)
            await Page.ClickAsync("label[for='legacyToggle']");
            await Page.WaitForSelectorAsync("#legacyNotice", new() { State = WaitForSelectorState.Visible });

            // Set Date to Dec 2025 (matching our replication target)
            await Page.FillAsync("input[name='Date']", "2025-12-02");

            await Page.FillAsync("#VoyageNumber", "V-LEGACY-1224");

            // FILL BILLING NUMBER (Documented)
            var billingNo = $"L-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#billingNumber", billingNo);

            // Wait for tickets to load (Legacy mode loads by customer)
            var ticketCheckboxes = Page.Locator("#ticketsTable tbody tr .ticket-checkbox");
            await ticketCheckboxes.First.WaitForAsync();
            
            // Ensure we start from an unchecked state to be surgical
            var selectAll = Page.Locator("#selectAllTickets");
            await selectAll.SetCheckedAsync(false);
            
            // Now select exactly 2 tickets
            await ticketCheckboxes.Nth(0).SetCheckedAsync(true);
            await ticketCheckboxes.Nth(1).SetCheckedAsync(true);

            // Wait for JS to update totals
            await Page.WaitForFunctionAsync(@"() => {
                const total = document.querySelector('#OverallTotal');
                return total && total.innerText !== '0.00' && total.innerText !== '0';
            }");

            // Ensure Vatable is correctly checked based on Customer (INSULAR OIL is Vatable)
            await Page.WaitForFunctionAsync("() => document.querySelector('#isVatableToggle').checked");

            // Verify calculation logic
            // 1. Check Exclusive (should be total * 1.12)
            if (await Page.IsCheckedAsync("#isVatInclusiveToggle"))
            {
                await Page.ClickAsync("#isVatInclusiveToggle");
                await Page.WaitForFunctionAsync("() => !document.querySelector('#isVatInclusiveToggle').checked");
            }
            
            await Page.WaitForTimeoutAsync(500);
            
            var dispatchTotalText = await Page.Locator("#DispatchAmountTotal").InnerTextAsync();
            var dispatchTotal = decimal.Parse(dispatchTotalText.Replace(",", "").Trim());

            var bafTotalText = await Page.Locator("#BAFAmountTotal").InnerTextAsync();
            var bafTotal = decimal.Parse(bafTotalText.Replace(",", "").Trim());
            
            var overallTotalText = await Page.Locator("#OverallTotal").InnerTextAsync();
            var overallTotal = decimal.Parse(overallTotalText.Replace(",", "").Trim());
            
            // With VAT Exclusive, Overall should be 1.12 * (Dispatch + BAF)
            Assert.Equal(Math.Round((dispatchTotal + bafTotal) * 1.12m, 2), Math.Round(overallTotal, 2));

            // 2. Toggle Inclusive (should be total == dispatch + baf) - This matches the Legacy target
            await Page.ClickAsync("#isVatInclusiveToggle");
            await Page.WaitForFunctionAsync("() => document.querySelector('#isVatInclusiveToggle').checked");
            await Page.WaitForTimeoutAsync(500);
            
            overallTotalText = await Page.Locator("#OverallTotal").InnerTextAsync();
            overallTotal = decimal.Parse(overallTotalText.Replace(",", "").Trim());
            
            Assert.Equal(dispatchTotal + bafTotal, overallTotal); // Inclusive means Amount == Ticket Sums

            // Submit
            await Page.ClickAsync("#submitButton");
            await Page.Locator(".swal2-confirm:has-text('Yes, submit')").ClickAsync();
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.Locator(".swal2-confirm:has-text('OK')").ClickAsync();
            });

            // 2. Collection
            await Page.GotoAsync($"{ServerAddress}/User/Collection/Create");
            await Page.EvaluateAsync("document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove())");

            await Page.FillAsync("#CustomerSearch", "INSULAR OIL");
            await Page.RunAndWaitForResponseAsync(async () =>
            {
                await Page.ClickAsync("#CustomerSearchResults .modern-dropdown-item:has-text('INSULAR OIL CORPORATION')");
            }, r => r.Url.Contains("GetUncollectedBillingsForTable") && r.Status == 200);

            await Page.WaitForTimeoutAsync(1000);

            // Use search to find our legacy billing
            await Page.FillAsync("input[type='search']", billingNo);
            await Page.WaitForTimeoutAsync(500);

            var billRow = Page.Locator("#billingsTable tbody tr").Filter(new() { HasText = billingNo });
            await billRow.ClickAsync();

            await Page.WaitForFunctionAsync(@"() => {
                const netDisplay = document.querySelector('#netAmountDisplay');
                return netDisplay && netDisplay.innerText !== '₱ 0.00' && netDisplay.innerText !== '₱0.00';
            }");

            var collectionNo = $"OR-L-{Guid.NewGuid().ToString().Substring(0, 8)}";
            await Page.FillAsync("#msapCollectionNumber", collectionNo);

            await SelectModernOptionAsync("Deposit To Bank", "MBTC 167-7-16753668-5 MALAYAN MARITIME SERVICES INC.");

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
