using VaryoCms.IntegrationTests.Infrastructure;

namespace VaryoCms.UiTests.Infrastructure;

/// <summary>
/// Combined fixture for UI tests: Testcontainers DB + real Kestrel app + Playwright browser.
/// Initialization order: DB → App → Browser.
/// </summary>
public sealed class UiTestFixture : IAsyncLifetime
{
    private readonly DatabaseFixture _db = new();
    private KestrelWebAppFactory? _appFactory;
    private readonly PlaywrightFixture _playwright = new();

    public string ServerAddress => _appFactory?.ServerAddress
        ?? throw new InvalidOperationException("App not started.");

    public PlaywrightFixture Playwright => _playwright;

    public async Task InitializeAsync()
    {
        // 1. Start SQL Server container and run migrations.
        await _db.InitializeAsync();

        // 2. Start the real Kestrel host pointing at the test DB.
        _appFactory = new KestrelWebAppFactory(_db.ConnectionString);
        await _appFactory.InitializeAsync();

        // 3. Launch headless Chromium.
        await _playwright.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _playwright.DisposeAsync();
        if (_appFactory is not null) await _appFactory.DisposeAsync();
        await _db.DisposeAsync();
    }
}

[CollectionDefinition("ui")]
public sealed class UiCollection : ICollectionFixture<UiTestFixture> { }
