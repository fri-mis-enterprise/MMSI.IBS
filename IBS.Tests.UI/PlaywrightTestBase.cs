using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace IBS.Tests.UI
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private IHost? _host;
        public string? ServerAddress { get; private set; }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            var dummyHost = builder.Build();

            builder.ConfigureWebHost(webHostBuilder =>
            {
                // Use 127.0.0.1 instead of localhost for dynamic port binding
                webHostBuilder.UseKestrel(options => options.Listen(System.Net.IPAddress.Loopback, 0));
            });

            _host = builder.Build();
            _host.Start();

            var server = _host.Services.GetRequiredService<IServer>();
            var addressFeature = server.Features.Get<IServerAddressesFeature>();
            ServerAddress = addressFeature!.Addresses.First();

            return dummyHost;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _host?.Dispose();
            }
        }
    }

    public abstract class PlaywrightTestBase : IClassFixture<CustomWebApplicationFactory<Program>>, IAsyncLifetime
    {
        protected readonly CustomWebApplicationFactory<Program> Factory;
        protected IPlaywright Playwright = null!;
        protected IBrowser Browser = null!;
        protected IBrowserContext Context = null!;
        protected IPage Page = null!;

        protected PlaywrightTestBase(CustomWebApplicationFactory<Program> factory)
        {
            Factory = factory;
        }

        public virtual async Task InitializeAsync()
        {
            // Trigger host creation
            _ = Factory.Server;

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                SlowMo = 100, // Increased SlowMo for better stability
                Args = new[] { "--start-maximized" }
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport
            });

            Page = await Context.NewPageAsync();
            Page.SetDefaultTimeout(30000); // 30s default timeout
        }


        public virtual async Task DisposeAsync()
        {
            if (Page != null) await Page.CloseAsync();
            if (Context != null) await Context.DisposeAsync();
            if (Browser != null) await Browser.DisposeAsync();
            Playwright?.Dispose();
        }

        protected string ServerAddress => Factory.ServerAddress ?? throw new Exception("Server address not set");

        protected async Task LoginAsync(string username, string password)
        {
            await Page.GotoAsync($"{ServerAddress}/Identity/Account/Login");
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            await Page.FillAsync("#username", username);
            await Page.FillAsync("input[name='Input.Password']", password);
            await Page.ClickAsync("#login-submit");

            await Page.WaitForURLAsync($"{ServerAddress}/", new PageWaitForURLOptions { Timeout = 15000 });
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        protected async Task SelectModernOptionAsync(string label, string optionText)
        {
            var idMap = new Dictionary<string, string>
            {
                { "Customer", "#CustomerContainer" },
                { "Deposit To Bank", "#BankContainer" },
                { "Port", "#PortContainer" },
                { "Terminal", "#TerminalContainer" },
                { "Vessel", "#VesselContainer" },
                { "Activity/Service Type", "#ServiceContainer" },
                { "Tugboat/Service provider", "#TugboatContainer" },
                { "Master on Duty", "#TugMasterContainer" }
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
            await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await trigger.ClickAsync(new() { Force = true });

            var dropdown = Page.Locator(".modern-select-dropdown.show");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var searchInput = dropdown.Locator(".modern-select-search input");
            if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
            {
                await searchInput.FillAsync(optionText);
                await Page.WaitForTimeoutAsync(500); // Wait for filter to settle
            }

            var escapedText = Regex.Escape(optionText);
            var option = dropdown.Locator(".modern-select-option")
                .Filter(new() { HasTextRegex = new Regex(escapedText, RegexOptions.IgnoreCase) })
                .First;

            await option.ScrollIntoViewIfNeededAsync();
            await ForceClickAsync(option);

            // Wait for dropdown to close
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        }

        protected async Task ForceClickAsync(ILocator locator)
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await locator.DispatchEventAsync("click");
            await Page.WaitForTimeoutAsync(200); // Settle time
        }

        protected async Task ConfirmSweetAlertAsync(string? buttonText = null)
        {
            // Wait for Swal container to exist
            var container = Page.Locator(".swal2-container");
            await container.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await Page.WaitForTimeoutAsync(500); // Animation buffer

            var button = Page.Locator(".swal2-confirm");
            if (!string.IsNullOrEmpty(buttonText))
            {
                var regex = new Regex(buttonText, RegexOptions.IgnoreCase);
                var textFilter = Page.Locator(".swal2-confirm").Filter(new() { HasTextRegex = regex });
                if (await textFilter.CountAsync() > 0)
                {
                    button = textFilter;
                }
            }

            await ForceClickAsync(button);
            await Page.WaitForTimeoutAsync(300);
        }

        protected async Task ClickSweetAlertOkAsync()
        {
            await ConfirmSweetAlertAsync("OK");
        }

        protected async Task DismissAnySweetAlertAsync()
        {
            var container = Page.Locator(".swal2-container");
            if (await container.CountAsync() > 0)
            {
                var confirmBtn = Page.Locator(".swal2-confirm");
                if (await confirmBtn.IsVisibleAsync())
                {
                    await ForceClickAsync(confirmBtn);
                    await container.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
                }
            }
        }
    }
}
