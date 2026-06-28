using VaryoCms.Application.DTOs.Api;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/api-management")]
public class ApiManagementController : Controller
{
    private readonly IApiConfigurationService _service;

    public ApiManagementController(IApiConfigurationService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var result = await _service.GetListAsync(ct);
        return View(result.Value ?? Array.Empty<ApiConfigListItemDto>());
    }

    [HttpGet("{contentTypeId:int}")]
    public async Task<IActionResult> Configure(int contentTypeId, CancellationToken ct)
    {
        var result = await _service.GetForEditAsync(contentTypeId, ct);
        if (!result.IsSuccess) return NotFound();
        return View(ApiConfigFormViewModel.FromDto(result.Value!));
    }

    [HttpPost("{contentTypeId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Configure(int contentTypeId, ApiConfigFormViewModel vm, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            var result = await _service.SaveAsync(vm.ToRequest(), ct);
            if (result.IsSuccess) return RedirectToAction(nameof(Configure), new { contentTypeId });
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(vm);
    }
}
