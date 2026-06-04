using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Playwright;
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
                Headless = false, // Set to false to see the browser
                SlowMo = 200,      // Slows down actions by 200ms to make them visible
                Args = new[] { "--start-maximized" }
            });

            Context = await Browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = ViewportSize.NoViewport // This allows --start-maximized to work
            });

            Page = await Context.NewPageAsync();
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

            // Use the correct selectors found in Login.cshtml
            await Page.FillAsync("#username", username);
            await Page.FillAsync("input[name='Input.Password']", password);
            await Page.ClickAsync("#login-submit");

            // Wait for navigation back to home page or specific dashboard element
            await Page.WaitForURLAsync($"{ServerAddress}/", new PageWaitForURLOptions { Timeout = 10000 });
        }
    }
}
