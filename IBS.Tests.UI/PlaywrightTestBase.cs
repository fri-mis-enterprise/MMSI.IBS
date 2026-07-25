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

        protected async Task SelectModernOptionAsync(string labelOrTestId, string optionText)
        {
            ILocator trigger;
            if (labelOrTestId.StartsWith("select-"))
            {
                trigger = Page.Locator($"[data-testid='{labelOrTestId}-trigger'], .modern-select-trigger[data-testid='{labelOrTestId}']").First;
            }
            else
            {
                var idMap = new Dictionary<string, string>
                {
                    { "Customer", "#CustomerContainer .modern-select-trigger, [data-testid='select-CustomerId-trigger']" },
                    { "Deposit To Bank", "#BankContainer .modern-select-trigger, [data-testid='select-BankAccountId-trigger']" },
                    { "Port", "#PortContainer .modern-select-trigger, [data-testid='select-PortId-trigger']" },
                    { "Terminal", "#TerminalContainer .modern-select-trigger, [data-testid='select-TerminalId-trigger']" },
                    { "Vessel", "#VesselContainer .modern-select-trigger, [data-testid='select-VesselId-trigger']" },
                    { "Activity/Service Type", "#ServiceContainer .modern-select-trigger, [data-testid='select-ServiceId-trigger']" },
                    { "Tugboat/Service provider", "#TugboatContainer .modern-select-trigger, [data-testid='select-TugboatId-trigger']" },
                    { "Master on Duty", "#TugMasterContainer .modern-select-trigger, [data-testid='select-TugMasterId-trigger']" }
                };

                if (idMap.TryGetValue(labelOrTestId, out var testIdSelector))
                {
                    trigger = Page.Locator(testIdSelector).First;
                }
                else
                {
                    trigger = Page.Locator("div")
                        .Filter(new() { Has = Page.Locator($"label:has-text('{labelOrTestId}')") })
                        .Filter(new() { Has = Page.Locator(".modern-select-trigger") })
                        .First
                        .Locator(".modern-select-trigger");
                }
            }

            await trigger.ClickAsync(new() { Force = true });

            var dropdown = Page.Locator(".modern-select-dropdown.show");
            await dropdown.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            var searchInput = dropdown.Locator(".modern-select-search input");
            if (await searchInput.CountAsync() > 0 && await searchInput.IsVisibleAsync())
            {
                await searchInput.FillAsync(optionText);
            }

            var option = dropdown.Locator(".modern-select-option")
                .Filter(new() { HasText = optionText })
                .First;

            await option.ClickAsync(new() { Force = true });
        }

        protected async Task ForceClickAsync(ILocator locator)
        {
            await locator.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 15000 });
            await locator.ClickAsync(new() { Force = true });
        }

        protected async Task ConfirmSweetAlertAsync(string? buttonText = null)
        {
            var container = Page.Locator(".swal2-container");
            await container.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 10000 });

            var confirmButton = container.Locator("[data-testid='swal-confirm-btn'], button.swal2-confirm").First;
            await confirmButton.ClickAsync(new() { Force = true });
            await container.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
        }

        protected async Task ClickSweetAlertOkAsync()
        {
            await ConfirmSweetAlertAsync("OK");
        }

        protected async Task DismissAnySweetAlertAsync()
        {
            var container = Page.Locator(".swal2-container");
            if (await container.CountAsync() > 0 && await container.IsVisibleAsync())
            {
                var confirmButton = container.Locator("[data-testid='swal-confirm-btn'], button.swal2-confirm").First;
                if (await confirmButton.CountAsync() > 0)
                {
                    await confirmButton.ClickAsync(new() { Force = true });
                    await container.WaitForAsync(new() { State = WaitForSelectorState.Hidden, Timeout = 3000 });
                }
            }
        }
    }
}
