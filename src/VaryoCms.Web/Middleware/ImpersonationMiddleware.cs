using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using VaryoCms.Web.Contexts;

namespace VaryoCms.Web.Middleware;

// Runs AFTER UseAuthentication: if the authenticated SystemAdmin carries an ImpersonatedTenantId claim,
// override the request's tenant (and language) context to that tenant. Every tenant-scoped repository then
// operates on the impersonated tenant, so the whole existing admin panel works unchanged.
public class ImpersonationMiddleware
{
    private readonly RequestDelegate _next;

    public ImpersonationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        TenantContext tenantContext,
        LanguageContext languageContext,
        ITenantStore tenantStore)
    {
        // Only honour the claim for a genuine SystemAdmin (the claim is signed, but defence in depth).
        if (context.User.IsInRole(nameof(UserRole.SystemAdmin))
            && int.TryParse(context.User.FindFirst("ImpersonatedTenantId")?.Value, out int tenantId))
        {
            var tenant = await tenantStore.FindByIdAsync(tenantId, context.RequestAborted);
            if (tenant is not null)
            {
                tenantContext.Set(tenant.Id, tenant.Slug, tenant.DefaultLanguageCode);
                // Keep an explicit ?lang choice; otherwise use the impersonated tenant's default language.
                if (string.IsNullOrWhiteSpace(context.Request.Query["lang"]))
                    languageContext.Set(tenant.DefaultLanguageCode);
            }
        }

        await _next(context);
    }
}
