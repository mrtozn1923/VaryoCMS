using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using VaryoCms.Web.ViewModels.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.System;

[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system/tenants")]
public class SystemTenantsController : Controller
{
    private readonly ISystemTenantService _tenants;

    public SystemTenantsController(ISystemTenantService tenants) => _tenants = tenants;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page, CancellationToken ct)
    {
        var result = await _tenants.GetListAsync(page < 1 ? 1 : page, 25, ct);
        var p = result.Value!;
        return View(new TenantListViewModel
        {
            Items = p.Items, Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages
        });
    }

    [HttpGet("create")]
    public IActionResult Create() => View(new TenantFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TenantFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _tenants.CreateAsync(vm.ToCreateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(vm);
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _tenants.GetForEditAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return View(TenantFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TenantFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _tenants.UpdateAsync(id, vm.ToUpdateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        vm.Id = id;
        return View(vm);
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tenants.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
