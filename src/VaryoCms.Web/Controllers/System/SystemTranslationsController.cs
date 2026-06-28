using System.Text;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Enums;
using VaryoCms.Web.ViewModels.System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Web.Controllers.System;

[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system/translations")]
public class SystemTranslationsController : Controller
{
    private readonly ISystemTranslationService _translations;
    private readonly IStringLocalizer<SharedResource> _t;

    public SystemTranslationsController(ISystemTranslationService translations, IStringLocalizer<SharedResource> t)
    {
        _translations = translations;
        _t = t;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? q, int page, CancellationToken ct)
        => View(await _translations.GetListAsync(q, page < 1 ? 1 : page, 30, ct));

    [HttpGet("edit")]
    public async Task<IActionResult> Edit(string key, CancellationToken ct)
    {
        var result = await _translations.GetKeyAsync(key, ct);
        if (!result.IsSuccess) return NotFound();
        var cultures = await _translations.GetCulturesAsync(ct);
        return View(new TranslationEditViewModel
        {
            Key = key,
            Cultures = cultures,
            Values = cultures.ToDictionary(
                c => c.Code,
                c => result.Value!.Values.TryGetValue(c.Code, out var v) ? v : string.Empty)
        });
    }

    [HttpPost("edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string key, Dictionary<string, string> values, CancellationToken ct)
    {
        var result = await _translations.SaveKeyAsync(key, values ?? new(), ct);
        TempData[result.IsSuccess ? "Msg" : "Error"] = result.IsSuccess ? _t["Msg.Saved"].Value : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("cultures")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddCulture(string code, string name, CancellationToken ct)
    {
        var result = await _translations.AddCultureAsync(code, name, ct);
        TempData[result.IsSuccess ? "Msg" : "Error"] = result.IsSuccess ? _t["Msg.LanguageAdded"].Value : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(string culture, CancellationToken ct)
    {
        var json = await _translations.ExportAsync(culture, ct);
        return File(Encoding.UTF8.GetBytes(json), "application/json", $"ui-translations-{culture}.json");
    }

    [HttpPost("import")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(string culture, string json, CancellationToken ct)
    {
        var result = await _translations.ImportAsync(culture, json, ct);
        TempData[result.IsSuccess ? "Msg" : "Error"] =
            result.IsSuccess ? string.Format(_t["Msg.Imported"].Value, result.Value) : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
