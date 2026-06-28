using VaryoCms.Application.DTOs.ContentType;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/content-types")]
public class ContentTypeController : Controller
{
    private readonly IContentTypeService _service;

    public ContentTypeController(IContentTypeService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        return View(result.Value ?? Array.Empty<ContentTypeDto>());
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var vm = new ContentTypeFormViewModel();
        vm.ParentOptions = await BuildParentOptionsAsync(excludeId: null, ct);
        return View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ContentTypeFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: null, ct);
            return View(vm);
        }

        var result = await _service.CreateAsync(vm.ToCreateRequest(), ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: null, ct);
            return View(vm);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _service.GetByIdAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        var vm = ContentTypeFormViewModel.FromDto(result.Value!);
        vm.ParentOptions = await BuildParentOptionsAsync(excludeId: id, ct);
        return View(vm);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ContentTypeFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: id, ct);
            return View(vm);
        }

        var result = await _service.UpdateAsync(id, vm.ToUpdateRequest(), ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            vm.ParentOptions = await BuildParentOptionsAsync(excludeId: id, ct);
            return View(vm);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> BuildParentOptionsAsync(int? excludeId, CancellationToken ct)
    {
        var all = (await _service.GetAllAsync(ct)).Value ?? [];
        var options = all
            .Where(c => c.Id != excludeId && (excludeId == null || c.ParentId != excludeId))
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
        options.Insert(0, new SelectListItem("— Üst yok —", ""));
        return options;
    }

    // Used by the field-options modal to populate the relation target-type dropdown.
    [HttpGet("json")]
    public async Task<IActionResult> ListJson(CancellationToken ct)
    {
        var result = await _service.GetAllAsync(ct);
        var items = (result.Value ?? []).Select(x => new { x.Id, x.Name, x.Slug });
        return Json(items);
    }
}
