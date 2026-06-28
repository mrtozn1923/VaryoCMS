using Microsoft.Playwright;
using VaryoCms.IntegrationTests.Infrastructure;
using VaryoCms.UiTests.Infrastructure;

namespace VaryoCms.UiTests.Tests;

/// <summary>
/// E2E tests for Content Type CRUD via the admin panel.
/// </summary>
[Collection("ui")]
public sealed class ContentTypeTests
{
    private readonly UiTestFixture _fixture;

    public ContentTypeTests(UiTestFixture fixture) => _fixture = fixture;

    private string Url(string path) => $"{_fixture.ServerAddress}{path}";

    [Fact]
    public async Task ContentType_list_page_loads_for_admin()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Url("/admin/content-types"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        string html = await page.ContentAsync();
        // Page should contain the CMS shell elements
        Assert.True(
            html.Contains("cms-sidebar", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("cms-table", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("İçerik", StringComparison.OrdinalIgnoreCase),
            "Expected content-types list page content.");
    }

    [Fact]
    public async Task ContentType_create_form_loads()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Url("/admin/content-types/create"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Form inputs should be present
        int nameInputs = await page.Locator("input[name='Name']").CountAsync();
        Assert.True(nameInputs > 0, "Expected Name input on create form.");

        int slugInputs = await page.Locator("input[name='Slug']").CountAsync();
        Assert.True(slugInputs > 0, "Expected Slug input on create form.");
    }

    [Fact]
    public async Task ContentType_create_persists_and_appears_in_list()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        // Shorter name and slug to stay within DB column limits and be clearly unique
        string uid = Guid.NewGuid().ToString("N")[..8];
        string uniqueName = $"Test CT {uid}";
        string uniqueSlug = $"test-ct-{uid}";

        await page.GotoAsync(Url("/admin/content-types/create"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await page.FillAsync("input[name='Name']", uniqueName);
        // Slug may auto-fill; clear and set explicitly
        await page.FillAsync("input[name='Slug']", uniqueSlug);

        // Use cms-btn-primary selector to target the form's Save/Create button,
        // NOT the layout's logout button (which is also type="submit" but class="cms-action-danger").
        await page.ClickAsync("button.cms-btn-primary[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Navigate to list (controller redirects there on success; we navigate explicitly in case
        // the WaitForLoadState already moved us there)
        await page.GotoAsync(Url("/admin/content-types"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        string html = await page.ContentAsync();
        Assert.Contains(uniqueName, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ContentType_create_form_requires_name_and_slug()
    {
        // Verify the create form itself has the required Name and Slug inputs
        // (client-side validation attributes) — prevents submitting an empty form.
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        await page.GotoAsync(Url("/admin/content-types/create"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Both Name and Slug inputs should have a required attribute (or data-val-required)
        string nameInputHtml = await page.Locator("input[name='Name']").First.EvaluateAsync<string>("el => el.outerHTML");
        string slugInputHtml = await page.Locator("input[name='Slug']").First.EvaluateAsync<string>("el => el.outerHTML");

        // The inputs should have validation attributes (required or data-val)
        bool nameRequired = nameInputHtml.Contains("required", StringComparison.OrdinalIgnoreCase)
            || nameInputHtml.Contains("data-val", StringComparison.OrdinalIgnoreCase);
        bool slugRequired = slugInputHtml.Contains("required", StringComparison.OrdinalIgnoreCase)
            || slugInputHtml.Contains("data-val", StringComparison.OrdinalIgnoreCase);

        Assert.True(nameRequired, $"Expected Name input to have validation attributes. HTML: {nameInputHtml}");
        Assert.True(slugRequired, $"Expected Slug input to have validation attributes. HTML: {slugInputHtml}");
    }
}
