using System.Net;
using System.Net.Http.Json;
using VaryoCms.IntegrationTests.Infrastructure;

namespace VaryoCms.IntegrationTests.Tests.Api;

/// <summary>
/// Integration tests for the public REST API (/api/v1/{tenantSlug}/{ctSlug}).
/// These hit a real SQL Server container via Testcontainers.
/// The dev-tenant and seed data are provisioned by 001+002 migrations.
/// </summary>
[Collection("integration")]
public sealed class PublicApiTests : IAsyncLifetime
{
    private readonly DatabaseFixture _db;
    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public PublicApiTests(DatabaseFixture db) => _db = db;

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(_db.ConnectionString);
        _client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Unknown_tenant_in_api_path_returns_404()
    {
        var response = await _client.GetAsync("/api/v1/nonexistent-tenant-xyz/any-ct");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_content_type_slug_returns_404()
    {
        // dev-tenant exists (seeded), but this CT slug doesn't
        var response = await _client.GetAsync("/api/v1/dev-tenant/nonexistent-content-type-xyz");

        // Either 404 (config not found) or 404 (CT not enabled for API)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Api_route_bypasses_tenant_host_middleware()
    {
        // /api/* routes skip host-based tenant resolution — they use tenantSlug from URL.
        // This ensures the middleware bypass is active.
        var response = await _client.GetAsync("/api/v1/dev-tenant/dummy-ct");

        // 404 (CT not found) is the correct response — NOT a 500 from missing tenant context
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
