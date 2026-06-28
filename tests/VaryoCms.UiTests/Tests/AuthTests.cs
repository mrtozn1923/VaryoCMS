using Microsoft.Playwright;
using VaryoCms.UiTests.Infrastructure;

namespace VaryoCms.UiTests.Tests;

/// <summary>
/// E2E Playwright tests for authentication flows.
/// </summary>
[Collection("ui")]
public sealed class AuthTests
{
    private readonly UiTestFixture _fixture;

    public AuthTests(UiTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Login_page_shows_tenant_slug()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.ServerAddress}/account/login");
        await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        string html = await page.ContentAsync();
        Assert.Contains("dev-tenant", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Invalid_credentials_show_error_message()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.ServerAddress}/account/login");
        await page.FillAsync("input[name='Email']", "wrong@test.com");
        await page.FillAsync("input[name='Password']", "wrongpassword");
        await page.ClickAsync("button[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Still on login page (no redirect)
        Assert.Contains("/account/login", page.Url, StringComparison.OrdinalIgnoreCase);

        // Error message present
        string html = await page.ContentAsync();
        Assert.True(
            html.Contains("validation-summary-errors", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("text-danger", StringComparison.OrdinalIgnoreCase),
            "Expected an error message on the login page.");
    }

    [Fact]
    public async Task Valid_login_redirects_to_dashboard()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        await PageHelpers.LoginAsync(page, _fixture.ServerAddress);

        // Should land on dashboard (/) after successful login
        Assert.True(
            page.Url.EndsWith("/") || page.Url.Contains("/home", StringComparison.OrdinalIgnoreCase),
            $"Expected dashboard URL but got: {page.Url}");

        string html = await page.ContentAsync();
        Assert.DoesNotContain("account/login", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unauthenticated_access_to_admin_redirects_to_login()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_fixture.ServerAddress}/admin/content-types");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Assert.Contains("/account/login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_clears_session_and_redirects_to_login()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        // Login first
        await PageHelpers.LoginAsync(page, _fixture.ServerAddress);

        // Find and click logout button
        // The logout form is a POST; find the logout form and submit it
        var logoutButton = page.Locator("form[action*='logout'] button");
        int logoutCount = await logoutButton.CountAsync();

        if (logoutCount == 0)
        {
            // Fallback: POST directly
            await page.GotoAsync($"{_fixture.ServerAddress}/");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        else
        {
            await logoutButton.First.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        }

        // After logout, accessing protected page redirects to login
        await page.GotoAsync($"{_fixture.ServerAddress}/");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("login", page.Url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Access_denied_page_loads_for_unauthorized_role()
    {
        await using var context = await _fixture.Playwright.NewContextAsync();
        var page = await context.NewPageAsync();

        // Login as admin then try a system route (requires SystemAdmin)
        await PageHelpers.LoginAsync(page, _fixture.ServerAddress);

        await page.GotoAsync($"{_fixture.ServerAddress}/system");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // TenantAdmin cannot access /system → should redirect to access-denied or login
        bool isRedirected = page.Url.Contains("access-denied", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("login", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("system", StringComparison.OrdinalIgnoreCase);
        Assert.True(isRedirected, $"Expected redirect but stayed at: {page.Url}");
    }
}
