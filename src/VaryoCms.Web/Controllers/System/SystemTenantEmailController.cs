using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using VaryoCms.Domain.Interfaces.Repositories;
using VaryoCms.Web.ViewModels.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.System;

[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system/tenants/{tenantId:int}/email")]
public class SystemTenantEmailController : Controller
{
    private readonly ITenantEmailSettingsRepository _emailSettings;
    private readonly ISystemTenantService _tenants;

    public SystemTenantEmailController(ITenantEmailSettingsRepository emailSettings, ISystemTenantService tenants)
    {
        _emailSettings = emailSettings;
        _tenants = tenants;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int tenantId, CancellationToken ct)
    {
        var tenantResult = await _tenants.GetForEditAsync(tenantId, ct);
        if (!tenantResult.IsSuccess) return NotFound();

        var settings = await _emailSettings.GetByTenantIdAsync(tenantId, ct);
        TenantEmailSettingsViewModel vm;
        if (settings is not null)
        {
            vm = TenantEmailSettingsViewModel.FromEntity(settings, tenantResult.Value!.Name);
        }
        else
        {
            vm = new TenantEmailSettingsViewModel { TenantId = tenantId, TenantName = tenantResult.Value!.Name };
        }
        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int tenantId, TenantEmailSettingsViewModel vm, CancellationToken ct)
    {
        vm.TenantId = tenantId;
        if (!ModelState.IsValid) return View(vm);

        await _emailSettings.UpsertAsync(vm.ToEntity(), ct);
        TempData["Success"] = true;
        return RedirectToAction(nameof(Index), new { tenantId });
    }
}
