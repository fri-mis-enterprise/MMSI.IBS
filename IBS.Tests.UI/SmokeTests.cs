using Microsoft.Playwright;
using Xunit;

namespace IBS.Tests.UI
{
    public class SmokeTests : PlaywrightTestBase
    {
        public SmokeTests(CustomWebApplicationFactory<Program> factory) : base(factory)
        {
        }

        [Fact]
        public async Task Can_Login_And_Navigate_To_Dashboard()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");
            
            // Assert that we are on the dashboard
            var title = await Page.TitleAsync();
            Assert.Contains("Dashboard", title);
        }

        [Fact]
        public async Task Can_Create_Payment_Term()
        {
            await LoginAsync("admin@mmsi.com", "Admin123!");

            // Navigate directly to Create
            await Page.GotoAsync($"{ServerAddress}/User/PaymentTerms/Create");
            
            // Wait for the form to be ready
            await Page.WaitForSelectorAsync("form button[type='submit']");
            
            // HACK: Remove overlays and disable the spinner logic that might interfere
            await Page.EvaluateAsync(@"() => {
                document.querySelectorAll('.loader-container, #qa-panel, .qa-list-area').forEach(el => el.remove());
                if (typeof $ !== 'undefined') $('form').off('submit'); 
            }");

            var termCode = $"T{Guid.NewGuid().ToString().Substring(0, 5)}";
            
            // Fill the form
            await Page.FillAsync("input[name='TermsCode']", termCode);
            await Page.FillAsync("input[name='NumberOfDays']", "30");
            await Page.FillAsync("input[name='NumberOfMonths']", "0");

            // Submit and wait for navigation
#pragma warning disable CS0612 // Type or member is obsolete
            await Page.RunAndWaitForNavigationAsync(async () =>
            {
                await Page.ClickAsync("main button:has-text('Create')");
            }, new PageRunAndWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });
#pragma warning restore CS0612

            // Verify the new term is in the list
            // Use the search box to find it
            await Page.FillAsync("input[type='search']", termCode);
            
            // Wait for the table to filter
            var termLocator = Page.Locator($"table#paginatedTable >> text={termCode}");
            await termLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible, Timeout = 10000 });
            
            var isVisible = await termLocator.IsVisibleAsync();
            Assert.True(isVisible, $"New term {termCode} should be visible in the list after search.");
        }
    }
}
