using System.Security.Claims;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.System;

// Lets a SystemAdmin "become" a tenant by re-issuing the auth cookie with an ImpersonatedTenantId claim.
// ImpersonationMiddleware reads that claim to override the tenant context for subsequent requests.
[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system/impersonate")]
public class ImpersonationController : Controller
{
    private const string ImpersonatedTenantId = "ImpersonatedTenantId";

    private readonly ITenantStore _tenants;
    private readonly IAuditLogger _audit;

    public ImpersonationController(ITenantStore tenants, IAuditLogger audit)
    {
        _tenants = tenants;
        _audit = audit;
    }

    [HttpPost("{tenantId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int tenantId, CancellationToken ct)
    {
        var tenant = await _tenants.FindByIdAsync(tenantId, ct);
        if (tenant is null) return NotFound();

        await ReSignInAsync(impersonatedTenantId: tenantId);
        await _audit.LogAsync(AuditActions.SystemImpersonationStarted, "Tenant", tenantId,
            entityName: tenant.Name, tenantIdOverride: tenantId, ct: ct);
        return Redirect("/");   // into the normal admin panel, now scoped to the impersonated tenant
    }

    [HttpPost("exit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Exit(CancellationToken ct)
    {
        var claim = User.FindFirst(ImpersonatedTenantId);
        if (int.TryParse(claim?.Value, out int impersonatedId))
        {
            await _audit.LogAsync(AuditActions.SystemImpersonationExited, "Tenant", impersonatedId,
                tenantIdOverride: impersonatedId, ct: ct);
        }
        await ReSignInAsync(impersonatedTenantId: null);
        return Redirect("/system");
    }

    // Rebuilds the cookie from the current SystemAdmin claims, adding/removing only the impersonation claim.
    private async Task ReSignInAsync(int? impersonatedTenantId)
    {
        var claims = User.Claims
            .Where(c => c.Type != ImpersonatedTenantId)
            .Select(c => new Claim(c.Type, c.Value))
            .ToList();

        if (impersonatedTenantId is int id)
            claims.Add(new Claim(ImpersonatedTenantId, id.ToString()));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
