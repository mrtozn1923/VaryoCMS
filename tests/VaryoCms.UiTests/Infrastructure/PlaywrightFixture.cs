using Microsoft.Playwright;

namespace VaryoCms.UiTests.Infrastructure;

/// <summary>
/// Manages the Playwright browser lifetime. Shared across the UI test collection.
/// Each test creates its own BrowserContext (isolated cookies/storage).
/// </summary>
public sealed class PlaywrightFixture : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;

    public IBrowser Browser => _browser ?? throw new InvalidOperationException("Playwright not initialized.");

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            // Uncomment to see the browser during debugging:
            // Headless = false,
            // SlowMo = 100,
        });
    }

    public async Task DisposeAsync()
    {
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    /// <summary>
    /// Creates a new isolated browser context (fresh cookies/storage) for a single test.
    /// </summary>
    public async Task<IBrowserContext> NewContextAsync(bool acceptInsecureCerts = false)
    {
        return await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = acceptInsecureCerts,
        });
    }
}
