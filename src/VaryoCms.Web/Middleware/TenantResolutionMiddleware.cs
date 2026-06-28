using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.Contexts;

namespace VaryoCms.Web.Middleware;

// Resolves the tenant from the request host (subdomain), or DevTenantSlug on localhost.
// Sets the scoped TenantContext; returns 404 if the slug maps to no active tenant.
public class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        ITenantStore tenantStore,
        IConfiguration configuration)
    {
        string path = context.Request.Path.Value ?? string.Empty;

        // /system/* → SystemAdmin console (cross-tenant, no ITenantContext needed).
        // /api/*    → Public API resolves tenant from URL slug inside PublicApiService.
        if (path.StartsWith("/system", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/api/",   StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        string host = context.Request.Host.Host;
        string slug = ResolveSlug(host, configuration);

        TenantInfo? tenant = await tenantStore.FindBySlugAsync(slug, context.RequestAborted);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync($"Tenant not found for '{slug}'.");
            return;
        }

        tenantContext.Set(tenant.Id, tenant.Slug, tenant.DefaultLanguageCode);
        context.Items["TenantId"] = tenant.Id;

        await _next(context);
    }

    private static string ResolveSlug(string host, IConfiguration configuration)
    {
        if (host is "localhost" or "127.0.0.1")
            return configuration["DevTenantSlug"] ?? "dev-tenant";

        // {slug}.cms.yourdomain.com -> first label
        int dot = host.IndexOf('.');
        return dot > 0 ? host[..dot] : host;
    }
}
