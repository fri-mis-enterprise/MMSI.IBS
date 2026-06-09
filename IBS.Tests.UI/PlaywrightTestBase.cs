using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
using System.Text.RegularExpressions;
using Xunit;

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
            _ = Factory.Server;

            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[] { "--start-maximized" }
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport
            });

            Page = await Context.NewPageAsync();
            Page.SetDefaultTimeout(60000); 
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
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await Page.FillAsync("#username", username);
            await Page.FillAsync("input[name='Input.Password']", password);
            await Page.ClickAsync("#login-submit");

            await Page.WaitForURLAsync($"{ServerAddress}/", new PageWaitForURLOptions { Timeout = 30000 });
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
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
            await Page.WaitForTimeoutAsync(300);
            await trigger.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            bool shown = await Page.Locator(".modern-select-dropdown.show").IsVisibleAsync();

            int retries = 3;
            while (retries-- > 0 && !shown)
            {
                await trigger.FocusAsync();
                await trigger.ClickAsync(new() { Force = true });
                try
                {
                    await Page.Locator(".modern-select-dropdown.show").WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 2000 });
                    shown = true;
                }
                catch
                {
                    await trigger.EvaluateAsync("el => el.click()");
                    await Page.WaitForTimeoutAsync(500);
                    if (await Page.Locator(".modern-select-dropdown.show").IsVisibleAsync()) shown = true;
                }
            }

            var dropdown = Page.Locator(".modern-select-dropdown.show");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var searchInput = dropdown.Locator(".modern-select-search input");
            if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
            {
                await searchInput.ClearAsync();
                await searchInput.FillAsync(optionText);
                await Page.WaitForTimeoutAsync(500);
            }

            var escapedText = Regex.Escape(optionText);
            var option = dropdown.Locator(".modern-select-option")
                .Filter(new() { HasTextRegex = new Regex("^\\s*" + escapedText + "\\s*$", RegexOptions.IgnoreCase) });

            if (await option.CountAsync() == 0)
            {
                option = dropdown.Locator(".modern-select-option")
                    .Filter(new() { HasTextRegex = new Regex(escapedText, RegexOptions.IgnoreCase) })
                    .First;
            }
            else
            {
                option = option.First;
            }

            await option.ScrollIntoViewIfNeededAsync();
            await ForceClickAsync(option);

            try
            {
                await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
            }
            catch { /* ignore timeout */ }
        }

        protected async Task ForceClickAsync(ILocator locator)
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await locator.DispatchEventAsync("click");
            await Page.WaitForTimeoutAsync(200);
        }

        protected async Task ConfirmSweetAlertAsync(string? buttonText = null)
        {
            var container = Page.Locator(".swal2-container");
            try 
            {
                await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });
            }
            catch 
            {
                if (await container.CountAsync() == 0) return;
            }
            
            await Page.WaitForTimeoutAsync(500);

            ILocator? button = null;
            if (!string.IsNullOrEmpty(buttonText))
            {
                var strategies = new[] {
                    container.Locator("button.swal2-confirm").Filter(new() { HasTextRegex = new Regex($"^{buttonText}$", RegexOptions.IgnoreCase) }),
                    container.Locator("button.swal2-confirm").Filter(new() { HasTextRegex = new Regex(buttonText, RegexOptions.IgnoreCase) }),
                    container.Locator("button").Filter(new() { HasTextRegex = new Regex(buttonText, RegexOptions.IgnoreCase) })
                };

                foreach (var strategy in strategies)
                {
                    if (await strategy.CountAsync() > 0)
                    {
                        button = strategy.First;
                        break;
                    }
                }
            }

            button ??= container.Locator("button.swal2-confirm");

            if (await button.CountAsync() > 0)
            {
                await button.ScrollIntoViewIfNeededAsync();
                await button.ClickAsync(new() { Force = true });
            }

            try
            {
                await container.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
            }
            catch { }
        }

        protected async Task ClickSweetAlertOkAsync()
        {
            await ConfirmSweetAlertAsync("OK");
        }

        protected async Task DismissAnySweetAlertAsync()
        {
            try
            {
                var container = Page.Locator(".swal2-container");
                for (int i = 0; i < 3; i++)
                {
                    if (await container.CountAsync() > 0 && await container.IsVisibleAsync())
                    {
                        var okButton = container.Locator("button.swal2-confirm");
                        if (await okButton.CountAsync() > 0)
                        {
                            await okButton.ClickAsync(new() { Force = true });
                            try { await container.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 1000 }); } catch { }
                        }
                    }
                    await Page.WaitForTimeoutAsync(300);
                }
            }
            catch { }
        }
    }
}
