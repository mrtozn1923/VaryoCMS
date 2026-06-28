using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/dictionary")]
public class DictionaryController : Controller
{
    private readonly IDictionaryService _service;

    public DictionaryController(IDictionaryService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, string? category, int page, CancellationToken ct)
    {
        var result = await _service.GetListAsync(q, category, page < 1 ? 1 : page, 25, ct);
        var p = result.Value!;
        var languages = await _service.GetActiveLanguagesAsync(ct);
        return View(new DictionaryListViewModel
        {
            Items = p.Items, Search = q, Category = category,
            Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages,
            LanguageCount = languages.Count
        });
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
        => View(new DictionaryEntryFormViewModel { Languages = await _service.GetActiveLanguagesAsync(ct) });

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DictionaryEntryFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.CreateAsync(vm.ToRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        vm.Languages = await _service.GetActiveLanguagesAsync(ct);
        return View(vm);
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _service.GetForEditAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        var dto = result.Value!;
        return View(new DictionaryEntryFormViewModel
        {
            Id = dto.Id, KeyName = dto.KeyName, Category = dto.Category,
            Translations = dto.Translations.ToDictionary(kv => kv.Key, kv => (string?)kv.Value),
            Languages = await _service.GetActiveLanguagesAsync(ct)
        });
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DictionaryEntryFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.UpdateAsync(id, vm.ToRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Index));
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        vm.Languages = await _service.GetActiveLanguagesAsync(ct);
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
