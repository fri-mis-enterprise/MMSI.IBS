using Microsoft.Playwright;

namespace IBS.Tests.UI.Pages
{
    public class JobOrderCreatePage
    {
        private readonly IPage _page;
        private readonly string _baseUrl;

        public JobOrderCreatePage(IPage page, string baseUrl)
        {
            _page = page;
            _baseUrl = baseUrl;
        }

        public async Task NavigateAsync()
        {
            await _page.GotoAsync($"{_baseUrl}/User/JobOrder/Create");
            await _page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        public async Task SelectCustomerAsync(string optionText)
        {
            await SelectModernOptionByTestIdAsync("select-CustomerId", optionText);
        }

        public async Task SelectVesselAsync(string optionText)
        {
            await SelectModernOptionByTestIdAsync("select-VesselId", optionText);
        }

        /// <summary>
        /// Selects the Port and waits for the Terminal cascade to load, then selects the Terminal.
        /// Must be called instead of separate SelectPortAsync + SelectTerminalAsync.
        /// </summary>
        public async Task SelectPortAndTerminalAsync(string portText, string terminalText)
        {
            await _page.RunAndWaitForResponseAsync(
                async () => await SelectModernOptionByTestIdAsync("select-PortId", portText),
                r => r.Url.Contains("ChangeTerminal") && r.Status == 200);
            await SelectModernOptionByTestIdAsync("select-TerminalId", terminalText);
        }

        public async Task SetDatesAndTimesAsync(string date, string startTime, string endTime)
        {
            await _page.FillAsync("input[name='Date']", date);
            await _page.FillAsync("#PlannedStartTime", startTime);
            await _page.FillAsync("#PlannedEndTime", endTime);
        }

        public async Task SubmitAsync()
        {
            await _page.ClickAsync("#confirmCreateBtn", new Microsoft.Playwright.PageClickOptions { Force = true });
        }

        private async Task SelectModernOptionByTestIdAsync(string testId, string optionText)
        {
            var trigger = _page.Locator($"[data-testid='{testId}-trigger'], .modern-select-trigger[data-testid='{testId}']").First;
            await trigger.EvaluateAsync("el => el.click()");

            var dropdown = _page.Locator(".modern-select-dropdown.show");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var searchInput = dropdown.Locator(".modern-select-search input");
            if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
            {
                await searchInput.FillAsync(optionText);
            }

            var option = dropdown.Locator(".modern-select-option")
                .Filter(new() { HasTextRegex = new System.Text.RegularExpressions.Regex(System.Text.RegularExpressions.Regex.Escape(optionText), System.Text.RegularExpressions.RegexOptions.IgnoreCase) })
                .First;
            await option.ClickAsync(new() { Force = true });

            // Ensure change event propagates to dependent selects (Port -> Terminal cascade)
            var selectId = testId.Replace("select-", "");
            await _page.EvaluateAsync($"document.querySelector('#{selectId}')?.dispatchEvent(new Event('change', {{ bubbles: true }}))");
        }
    }
}
