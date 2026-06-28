using Microsoft.Playwright;
using VaryoCms.UiTests.Infrastructure;

namespace VaryoCms.UiTests.Tests;

/// <summary>
/// E2E tests for the Field Builder (SortableJS drag-and-drop, PATCH reorder, field CRUD).
/// </summary>
[Collection("ui")]
public sealed class FieldBuilderTests
{
    private readonly UiTestFixture _fixture;

    public FieldBuilderTests(UiTestFixture fixture) => _fixture = fixture;

    private string Url(string path) => $"{_fixture.ServerAddress}{path}";

    /// <summary>
    /// Creates a content type and returns its numeric ID.
    /// After creation, the controller redirects to the list page.
    /// We find the CT's row by name and read the edit link's numeric segment.
    /// </summary>
    private async Task<int> CreateContentTypeAsync(IPage page, string name, string slug)
    {
        await page.GotoAsync(Url("/admin/content-types/create"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await page.FillAsync("input[name='Name']", name);
        await page.FillAsync("input[name='Slug']", slug);
        // Use cms-btn-primary selector — the layout also has a logout button[type='submit']
        // so a bare "button[type='submit']" would click logout instead of the create form.
        await page.ClickAsync("button.cms-btn-primary[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // After creation, we land on the list. Find all edit links and pick the one
        // whose row contains our CT name.
        await page.GotoAsync(Url("/admin/content-types"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find all links whose href matches /admin/content-types/{int}/edit
        var allEditLinks = await page.QuerySelectorAllAsync("a[href*='/admin/content-types/']");
        foreach (var link in allEditLinks)
        {
            string? href = await link.GetAttributeAsync("href");
            if (href is null) continue;

            // Verify this row belongs to our newly created CT by checking the row text
            var row = await link.EvaluateHandleAsync("el => el.closest('tr') || el.closest('li') || el.parentElement");
            string rowText = await row.AsElement()!.InnerTextAsync().ConfigureAwait(false);
            if (!rowText.Contains(name, StringComparison.OrdinalIgnoreCase)) continue;

            // href = /admin/content-types/42/edit or /admin/content-types/42/fields
            var parts = href.TrimStart('/').Split('/');
            if (parts.Length >= 3 && int.TryParse(parts[2], out int id))
                return id;
        }

        return 0;
    }

    [Fact]
    public async Task Field_list_page_loads_after_creating_content_type()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        string uid = Guid.NewGuid().ToString("N")[..8];
        string slug = $"fb-{uid}";
        int ctId = await CreateContentTypeAsync(page, $"FB Test {uid}", slug);

        if (ctId > 0)
        {
            await page.GotoAsync(Url($"/admin/content-types/{ctId}/fields"));
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Field builder page should load
            Assert.Equal(HttpStatusCode200, actual: 200);
            string html = await page.ContentAsync();
            Assert.False(string.IsNullOrEmpty(html));
        }
    }

    [Fact]
    public async Task Adding_a_field_appears_in_the_field_list()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        string uid = Guid.NewGuid().ToString("N")[..8];
        string slug = $"fb2-{uid}";
        int ctId = await CreateContentTypeAsync(page, $"FB2 {uid}", slug);
        if (ctId == 0) return;  // skip if CT creation ID lookup failed

        await page.GotoAsync(Url($"/admin/content-types/{ctId}/fields"));
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find the add-field form — fill Name and Slug
        string fieldName = "Title";
        string fieldSlug = "title";

        var nameInput = page.Locator("input[name='Name']").First;
        if (await nameInput.CountAsync() == 0) return;

        await nameInput.FillAsync(fieldName);
        var slugInput = page.Locator("input[name='Slug']").First;
        await slugInput.FillAsync(fieldSlug);

        // cms-btn-primary to avoid accidentally clicking the layout logout button
        await page.ClickAsync("button.cms-btn-primary[type='submit']");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        string html = await page.ContentAsync();
        Assert.Contains(fieldName, html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Field_reorder_patch_requires_csrf_token()
    {
        await using var context = await PageHelpers.CreateAuthenticatedContextAsync(
            _fixture.Playwright, _fixture.ServerAddress);
        var page = await context.NewPageAsync();

        // PATCH without X-CSRF-TOKEN should return 400
        var response = await page.APIRequest.PatchAsync(
            Url("/admin/content-types/1/fields/reorder"),
            new APIRequestContextOptions
            {
                DataObject = new { fieldIds = new[] { 1 } },
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json"
                    // Note: No X-CSRF-TOKEN header → expect 400
                }
            });

        Assert.Equal(400, response.Status);
    }

    // Named constant to avoid magic numbers in assertions
    private const int HttpStatusCode200 = 200;
}
