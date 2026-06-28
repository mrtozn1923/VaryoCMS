using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/api-management/credentials")]
public class ApiCredentialController : Controller
{
    private readonly IApiCredentialService _service;

    public ApiCredentialController(IApiCredentialService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _service.GetListAsync(ct);
        return View(result.Value ?? Array.Empty<ApiCredentialListItemDto>());
    }

    [HttpGet("new")]
    public async Task<IActionResult> Create(CancellationToken ct)
    {
        var result = await _service.GetForEditAsync(null, ct);
        if (!result.IsSuccess) return NotFound();
        return View(ApiCredentialFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApiCredentialFormViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
        var result = await _service.SaveAsync(vm.ToRequest(), ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }
        if (!string.IsNullOrEmpty(result.Value!.PlaintextKey))
            TempData["NewSecret"] = result.Value.PlaintextKey;
        return RedirectToAction(nameof(Edit), new { id = result.Value.CredentialId });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var result = await _service.GetForEditAsync(id, ct);
        if (!result.IsSuccess) return NotFound();
        return View(ApiCredentialFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ApiCredentialFormViewModel vm, CancellationToken ct)
    {
        vm.Id = id;
        if (!ModelState.IsValid) return View(vm);
        var result = await _service.SaveAsync(vm.ToRequest(), ct);
        if (!result.IsSuccess)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View(vm);
        }
        TempData["SaveOk"] = true;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/rotate-key")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RotateKey(int id, CancellationToken ct)
    {
        var result = await _service.RotateApiKeyAsync(id, ct);
        TempData[result.IsSuccess ? "NewSecret" : "CredentialError"] =
            result.IsSuccess ? result.Value : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/generate-token")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateToken(int id, CancellationToken ct)
    {
        var result = await _service.IssueJwtAsync(id, ct);
        TempData[result.IsSuccess ? "NewSecret" : "CredentialError"] =
            result.IsSuccess ? result.Value : result.Error;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:int}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return RedirectToAction(nameof(Index));
    }
}
