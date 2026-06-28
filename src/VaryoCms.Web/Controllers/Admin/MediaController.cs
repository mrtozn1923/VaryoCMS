using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Web.Controllers.Admin;

// Media library management is restricted to TenantAdmin and SystemAdmin.
// The search endpoint is also accessible by Editors so the media picker works in content forms.
[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/media")]
public class MediaController : Controller
{
    private readonly IMediaService _media;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public MediaController(IMediaService media, IStringLocalizer<SharedResource> localizer)
    {
        _media = media;
        _localizer = localizer;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? type, int page, CancellationToken ct)
    {
        var list = await _media.GetListAsync(type, page < 1 ? 1 : page, 24, ct);
        var p = list.Value!;
        return View(new MediaLibraryViewModel
        {
            Items = p.Items, MediaType = type,
            Page = p.Page, PageSize = p.PageSize, Total = p.Total, TotalPages = p.TotalPages
        });
    }

    // Editors need this endpoint so the media picker works inside content edit forms.
    [HttpGet("search")]
    [Authorize(Roles = "TenantAdmin,SystemAdmin,Editor")]
    public async Task<IActionResult> Search(string? q, string? type, CancellationToken ct)
        => Json(await _media.SearchAsync(q, type, 20, ct));

    [HttpPost("upload")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file uploaded.");

        await using var stream = file.OpenReadStream();
        var result = await _media.UploadAsync(stream, file.FileName, file.ContentType, file.Length, ct);
        return result.IsSuccess ? Json(result.Value) : BadRequest(result.Error);
    }

    [HttpPost("{id:int}/meta")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateMeta(int id, string baseName, string? altText, CancellationToken ct)
    {
        var result = await _media.UpdateMetaAsync(id, baseName, altText, ct);
        if (!result.IsSuccess) return BadRequest(result.Error);
        return Ok();
    }

    [HttpPost("{id:int}/crop")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crop(int id, int x, int y, int width, int height, CancellationToken ct)
    {
        var result = await _media.CropAsync(id, x, y, width, height, ct);
        TempData[result.IsSuccess ? "MediaMsg" : "MediaErr"] = result.IsSuccess ? _localizer["Msg.ImageCropped"].Value : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _media.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
