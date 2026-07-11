using VaryoCms.Application.Interfaces;
using VaryoCms.Web.Contexts;

namespace VaryoCms.Web.Middleware;

// Runs AFTER UseAuthentication + ImpersonationMiddleware: binds the authenticated session to the tenant
// resolved for this request. The request's tenant comes from the subdomain (TenantResolutionMiddleware),
// but a signed-in user's own tenant lives in the cookie's TenantId claim. Nothing else re-checks that
// these agree, so if a tenant-A user's cookie ever reached tenant-B's host they would pass every
// role/permission check while repositories operate on tenant B's data. This closes that gap.
//
// Exemptions:
//  - Anonymous requests (login pages, public API, static errors) — nothing to bind yet.
//  - SystemAdmins — they legitimately operate cross-tenant (and use impersonation, which already
//    overrode ITenantContext to the target tenant above).
//  - Requests with no resolved tenant (TenantId <= 0) — TenantResolutionMiddleware skips
//    /system/* and /api/* (public API + API docs authenticate via credentials, not the session),
//    so there is no tenant to bind the session to; comparing against 0 would 403 every
//    signed-in tenant user who opens /api/docs or calls the public API from the same browser.
public class TenantAccessGuardMiddleware
{
    private readonly RequestDelegate _next;

    public TenantAccessGuardMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context, ICurrentUserContext currentUser, ITenantContext tenantContext)
    {
        if (currentUser.IsAuthenticated && !currentUser.IsSystemAdmin && tenantContext.TenantId > 0)
        {
            int? userTenant = currentUser.TenantId;
            if (userTenant is null || userTenant.Value != tenantContext.TenantId)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }
        }

        await _next(context);
    }
}
