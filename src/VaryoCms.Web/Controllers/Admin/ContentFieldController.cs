using VaryoCms.Application.DTOs.ContentField;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/content-types/{contentTypeId:int}/fields")]
public class ContentFieldController : Controller
{
    private readonly IContentFieldService _fields;
    private readonly IContentTypeService _types;

    public ContentFieldController(IContentFieldService fields, IContentTypeService types)
    {
        _fields = fields;
        _types = types;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int contentTypeId, CancellationToken ct)
    {
        var type = await _types.GetByIdAsync(contentTypeId, ct);
        if (!type.IsSuccess) return NotFound();

        var fields = await _fields.GetByContentTypeAsync(contentTypeId, ct);
        return View(new FieldBuilderViewModel
        {
            ContentTypeId = contentTypeId,
            ContentTypeName = type.Value!.Name,
            Fields = fields.Value!,
            NewField = new ContentFieldFormViewModel { ContentTypeId = contentTypeId }
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int contentTypeId, ContentFieldFormViewModel vm, CancellationToken ct)
    {
        vm.ContentTypeId = contentTypeId;
        var result = await _fields.CreateAsync(vm.ToCreateRequest(), ct);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int contentTypeId, int id, CancellationToken ct)
    {
        var result = await _fields.GetByIdAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return View(ContentFieldFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int contentTypeId, int id, ContentFieldFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _fields.UpdateAsync(id, vm.ToUpdateRequest(), ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }
        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int contentTypeId, int id, CancellationToken ct)
    {
        await _fields.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    [HttpPatch("reorder")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reorder(int contentTypeId, [FromBody] ReorderFieldsRequest req, CancellationToken ct)
    {
        var result = await _fields.ReorderAsync(contentTypeId, req, ct);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    // Used by the field-options modal to populate the relation display-field dropdown.
    [HttpGet("json")]
    public async Task<IActionResult> ListJson(int contentTypeId, CancellationToken ct)
    {
        var result = await _fields.GetByContentTypeAsync(contentTypeId, ct);
        var items = (result.Value ?? []).Select(x => new { x.Slug, x.Name });
        return Json(items);
    }
}
