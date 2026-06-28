using Microsoft.Playwright;

namespace VaryoCms.UiTests.Infrastructure;

/// <summary>
/// Shared helpers for Playwright page interactions (login, navigation, etc.)
/// </summary>
public static class PageHelpers
{
    /// <summary>
    /// Navigates to /account/login, fills credentials, and submits.
    /// EmailVerification must be disabled in test config (dev-tenant default).
    /// </summary>
    public static async Task LoginAsync(IPage page, string baseUrl,
        string email = "admin@dev.local", string password = "Admin123!")
    {
        await page.GotoAsync($"{baseUrl}/account/login");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='Email']", email);
        await page.FillAsync("input[name='Password']", password);
        await page.ClickAsync("button[type='submit']");

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>
    /// Creates an authenticated page context. Saves storageState to a temp file
    /// so it can be reused across tests in the same collection.
    /// </summary>
    public static async Task<IBrowserContext> CreateAuthenticatedContextAsync(
        PlaywrightFixture playwright, string baseUrl,
        string email = "admin@dev.local", string password = "Admin123!")
    {
        var context = await playwright.NewContextAsync();
        var page = await context.NewPageAsync();
        await LoginAsync(page, baseUrl, email, password);
        await page.CloseAsync();
        return context;
    }

    /// <summary>
    /// Waits for a CMS toast or success indicator after a form submission.
    /// </summary>
    public static async Task WaitForRedirectAsync(IPage page, string expectedUrlFragment)
    {
        await page.WaitForURLAsync(url => url.Contains(expectedUrlFragment),
            new PageWaitForURLOptions { Timeout = 10_000 });
    }
}
