using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.System;

[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system")]
public class SystemDashboardController : Controller
{
    private readonly ISystemTenantService _tenants;

    public SystemDashboardController(ISystemTenantService tenants) => _tenants = tenants;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _tenants.GetDashboardAsync(ct));
}
