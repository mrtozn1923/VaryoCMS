using System.Net;
using VaryoCms.IntegrationTests.Infrastructure;

namespace VaryoCms.IntegrationTests.Tests.Middleware;

/// <summary>
/// Verifies TenantResolutionMiddleware behavior against the real database.
/// - localhost → resolves to dev-tenant (DevTenantSlug config)
/// - /system/* and /api/* bypass tenant resolution
/// - Anonymous requests to protected pages redirect to login
/// </summary>
[Collection("integration")]
public sealed class TenantResolutionTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private CustomWebApplicationFactory _factory = null!;

    public TenantResolutionTests(DatabaseFixture db) => _db = db;

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(_db.ConnectionString);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Localhost_resolves_to_dev_tenant_and_login_page_returns_200()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/account/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("dev-tenant", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unauthenticated_request_to_protected_page_redirects_to_login()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/admin/content-types");

        // Expect a redirect to /account/login
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/account/login", response.Headers.Location?.ToString() ?? "");
    }

    [Fact]
    public async Task System_path_bypasses_tenant_resolution_and_shows_system_login()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = true,
            HandleCookies = true,
        });

        var response = await client.GetAsync("/system/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AntiForgery_enforced_on_login_post_without_token_returns_400()
    {
        var client = _factory.CreateClient();

        // POST without antiforgery token
        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Email",    "admin@dev.local"),
            new KeyValuePair<string, string>("Password", "Admin123!"),
        });

        var response = await client.PostAsync("/account/login", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Authenticated_admin_can_access_dashboard()
    {
        var client = await LoginHelper.CreateAdminClientAsync(_factory);

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
