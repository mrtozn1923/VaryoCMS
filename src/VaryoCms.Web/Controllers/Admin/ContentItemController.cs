using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.Authorization;
using VaryoCms.Web.Support;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

// Requires an authenticated user; per-action [RequireContentTypePermission] adds the content-type-level check.
[Authorize]
[Route("admin/content-types/{contentTypeId:int}/items")]
public class ContentItemController : Controller
{
    private readonly IContentItemService _items;
    private readonly IContentTypeService _types;
    private readonly ContentItemFormFactory _formFactory;
    private readonly ILanguageContext _language;
    private readonly IPermissionService _permissions;
    private readonly ILanguageService _languages;

    public ContentItemController(
        IContentItemService items, IContentTypeService types,
        ContentItemFormFactory formFactory, ILanguageContext language,
        IPermissionService permissions, ILanguageService languages)
    {
        _items = items;
        _types = types;
        _formFactory = formFactory;
        _language = language;
        _permissions = permissions;
        _languages = languages;
    }

    [HttpGet("")]
    [RequireContentTypePermission(ContentPermission.Read)]
    public async Task<IActionResult> Index(
        int contentTypeId, int page, string? q, string? status, string? filterLang, CancellationToken ct)
    {
        var type = await _types.GetByIdAsync(contentTypeId, ct);
        if (!type.IsSuccess) return NotFound();

        int currentPage = page < 1 ? 1 : page;
        var list = await _items.GetListAsync(
            contentTypeId, _language.CurrentCode, currentPage, 20, q, status, filterLang, ct);
        var p = list.Value!;
        var perms = await _permissions.GetPermissionsAsync(contentTypeId, ct);
        var langs = await _languages.GetActiveAsync(ct);

        return View(new ContentItemListViewModel
        {
            ContentTypeId = contentTypeId, ContentTypeName = type.Value!.Name,
            Items = p.Items, Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages,
            CanCreate = perms.CanCreate, CanUpdate = perms.CanUpdate, CanDelete = perms.CanDelete,
            Q = q, StatusFilter = status, LanguageFilter = filterLang,
            AvailableLanguages = langs
        });
    }

    // Relation picker search — contentTypeId here is the *target* content type being searched.
    [HttpGet("search")]
    [RequireContentTypePermission(ContentPermission.Read)]
    public async Task<IActionResult> Search(
        int contentTypeId, string? q, string? displayField, string? lang, CancellationToken ct)
    {
        var result = await _items.SearchRelatedAsync(contentTypeId, displayField, q, lang ?? _language.CurrentCode, ct);
        return Json(result.Value);
    }

    [HttpGet("create")]
    [RequireContentTypePermission(ContentPermission.Create)]
    public async Task<IActionResult> Create(int contentTypeId, string? lang, CancellationToken ct)
    {
        var vm = await _formFactory.BuildAsync(contentTypeId, null, lang ?? _language.CurrentCode, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    [RequireContentTypePermission(ContentPermission.Create)]
    public async Task<IActionResult> Create(int contentTypeId, ContentItemFormViewModel vm, CancellationToken ct)
    {
        vm.ContentTypeId = contentTypeId;
        var result = await _items.CreateAsync(vm.ToSaveRequest(), ct);
        if (result.IsSuccess) return RedirectToAction(nameof(Index), new { contentTypeId });

        TempData["Error"] = result.Error;
        var rebuilt = await _formFactory.BuildAsync(contentTypeId, null, vm.LanguageCode, ct);
        return rebuilt is null ? NotFound() : View(Merge(rebuilt, vm));
    }

    [HttpGet("{id:int}/edit")]
    [RequireContentTypePermission(ContentPermission.Update)]
    public async Task<IActionResult> Edit(int contentTypeId, int id, string? lang, CancellationToken ct)
    {
        var vm = await _formFactory.BuildAsync(contentTypeId, id, lang ?? _language.CurrentCode, ct);
        return vm is null ? NotFound() : View(vm);
    }

    [HttpPost("{id:int}/edit")]
    [ValidateAntiForgeryToken]
    [RequireContentTypePermission(ContentPermission.Update)]
    public async Task<IActionResult> Edit(int contentTypeId, int id, ContentItemFormViewModel vm, CancellationToken ct)
    {
        vm.ContentTypeId = contentTypeId;
        var result = await _items.UpdateAsync(id, vm.ToSaveRequest(), ct);
        if (result.IsSuccess) return RedirectToAction(nameof(Index), new { contentTypeId });

        TempData["Error"] = result.Error;
        var rebuilt = await _formFactory.BuildAsync(contentTypeId, id, vm.LanguageCode, ct);
        return rebuilt is null ? NotFound() : View(Merge(rebuilt, vm));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    [RequireContentTypePermission(ContentPermission.Delete)]
    public async Task<IActionResult> Delete(int contentTypeId, int id, CancellationToken ct)
    {
        await _items.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    private static ContentItemFormViewModel Merge(ContentItemFormViewModel rebuilt, ContentItemFormViewModel posted)
    {
        rebuilt.Status = posted.Status;
        rebuilt.Slug = posted.Slug;
        rebuilt.Title = posted.Title;
        rebuilt.IsLanguageActive = posted.IsLanguageActive;
        foreach (var f in rebuilt.Fields)
            f.Value = posted.Values.TryGetValue(f.Id, out var v) ? v : f.Value;
        return rebuilt;
    }
}
