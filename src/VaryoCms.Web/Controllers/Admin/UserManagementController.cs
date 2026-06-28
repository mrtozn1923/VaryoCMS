using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/users")]
public class UserManagementController : Controller
{
    private readonly IUserService _service;

    public UserManagementController(IUserService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(int page, CancellationToken ct)
    {
        var result = await _service.GetListAsync(page < 1 ? 1 : page, 25, ct);
        var p = result.Value!;
        return View(new UserListViewModel
        {
            Items = p.Items, Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages
        });
    }

    [HttpGet("create")]
    public IActionResult Create() => View(new UserFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(vm.ToCreateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(vm);
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _service.GetForEditAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return View(UserFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(id, vm.ToUpdateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(vm);
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
