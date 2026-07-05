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
    private readonly ContentItemSaveCoordinator _saveCoordinator;
    private readonly ILanguageContext _language;
    private readonly IPermissionService _permissions;
    private readonly ILanguageService _languages;

    public ContentItemController(
        IContentItemService items, IContentTypeService types,
        ContentItemFormFactory formFactory, ContentItemSaveCoordinator saveCoordinator, ILanguageContext language,
        IPermissionService permissions, ILanguageService languages)
    {
        _items = items;
        _types = types;
        _formFactory = formFactory;
        _saveCoordinator = saveCoordinator;
        _language = language;
        _permissions = permissions;
        _languages = languages;
    }

    [HttpGet("")]
    [RequireContentTypePermission(ContentPermission.Read)]
    public async Task<IActionResult> Index(
        int contentTypeId, int page, string? q, string? status, string? filterLang, bool unread, CancellationToken ct)
    {
        var type = await _types.GetByIdAsync(contentTypeId, ct);
        if (!type.IsSuccess) return NotFound();

        int currentPage = page < 1 ? 1 : page;
        var list = await _items.GetListAsync(
            contentTypeId, _language.CurrentCode, currentPage, 20, q, status, filterLang, unread, ct);
        var p = list.Value!;
        var perms = await _permissions.GetPermissionsAsync(contentTypeId, ct);
        var langs = await _languages.GetActiveAsync(ct);

        return View(new ContentItemListViewModel
        {
            ContentTypeId = contentTypeId, ContentTypeName = type.Value!.Name,
            IsInbox = type.Value!.NotifyOnCreate,
            Items = p.Items, Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages,
            CanCreate = perms.CanCreate, CanUpdate = perms.CanUpdate, CanDelete = perms.CanDelete,
            Q = q, StatusFilter = status, LanguageFilter = filterLang, UnreadOnly = unread,
            AvailableLanguages = langs
        });
    }

    // Okundu/okunmadı işaretle (yalnızca gelen-kutusu tipleri için anlamlı). Update yetkisi gerektirir.
    [HttpPost("{id:int}/read")]
    [ValidateAntiForgeryToken]
    [RequireContentTypePermission(ContentPermission.Update)]
    public async Task<IActionResult> SetRead(int contentTypeId, int id, bool isRead, CancellationToken ct)
    {
        var result = await _items.SetReadAsync(contentTypeId, id, isRead, ct);
        if (!result.IsSuccess) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index), new { contentTypeId });
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
        var result = await _saveCoordinator.CreateNewAsync(vm, ct);
        if (result.IsSuccess) return RedirectToAction(nameof(Index), new { contentTypeId });

        TempData["Error"] = FormatLanguageError(result);
        if (result.ItemPersisted)
            return RedirectToAction(nameof(Edit), new { contentTypeId, id = result.ItemId, lang = result.FailedLanguage });

        var rebuilt = await _formFactory.BuildAsync(contentTypeId, null, vm.PrimaryLanguageCode, ct);
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
        var result = await _saveCoordinator.SaveExistingAsync(id, vm, ct);
        if (result.IsSuccess) return RedirectToAction(nameof(Index), new { contentTypeId });

        TempData["Error"] = FormatLanguageError(result);
        var rebuilt = await _formFactory.BuildAsync(contentTypeId, id, result.FailedLanguage ?? vm.PrimaryLanguageCode, ct);
        return rebuilt is null ? NotFound() : View(Merge(rebuilt, vm));
    }

    private static string FormatLanguageError(ContentItemSaveResult result) =>
        result.FailedLanguage is null ? result.Error ?? "" : $"[{result.FailedLanguage.ToUpper()}] {result.Error}";

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    [RequireContentTypePermission(ContentPermission.Delete)]
    public async Task<IActionResult> Delete(int contentTypeId, int id, CancellationToken ct)
    {
        await _items.DeleteAsync(contentTypeId, id, ct);
        return RedirectToAction(nameof(Index), new { contentTypeId });
    }

    // Re-applies what the user just posted onto the freshly-rebuilt (from DB) view model, so a
    // validation failure re-renders exactly what they typed instead of stale/blank values —
    // across every language panel, not just the one that failed.
    private static ContentItemFormViewModel Merge(ContentItemFormViewModel rebuilt, ContentItemFormViewModel posted)
    {
        rebuilt.Status = posted.Status;
        rebuilt.Slug = posted.Slug;
        rebuilt.Titles = posted.Titles;
        rebuilt.LanguageActive = posted.LanguageActive;
        foreach (var f in rebuilt.Fields)
        {
            if (f.IsLocalized)
            {
                foreach (var lang in f.ValuesByLang.Keys.ToList())
                    if (posted.LocalizedValues.TryGetValue(lang, out var vals) && vals.TryGetValue(f.Id, out var v))
                        f.ValuesByLang[lang] = v;
            }
            else
            {
                f.Value = posted.Values.TryGetValue(f.Id, out var v) ? v : f.Value;
            }
        }
        return rebuilt;
    }
}
