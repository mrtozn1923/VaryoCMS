using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/languages")]
public class LanguageController : Controller
{
    private readonly ILanguageService _languages;

    public LanguageController(ILanguageService languages) => _languages = languages;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _languages.GetListAsync(ct));

    [HttpGet("create")]
    public IActionResult Create() => View(new LanguageFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LanguageFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _languages.CreateAsync(vm.ToCreateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(vm);
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _languages.GetForEditAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return View(LanguageFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, LanguageFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _languages.UpdateAsync(id, vm.ToUpdateRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        vm.Id = id;
        return View(vm);
    }

    [HttpPost("{id:int}/set-active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(int id, bool isActive, CancellationToken ct)
    {
        var result = await _languages.SetActiveAsync(id, isActive, ct);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index));
    }
}
