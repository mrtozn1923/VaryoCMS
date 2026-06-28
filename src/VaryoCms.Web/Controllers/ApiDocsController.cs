using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Web.Controllers;

[AllowAnonymous]
[Route("api/docs")]
public sealed class ApiDocsController : Controller
{
    private readonly IApiDocService _service;

    public ApiDocsController(IApiDocService service) => _service = service;

    [HttpGet("{tenantSlug}")]
    public IActionResult Index(string tenantSlug)
    {
        ViewData["TenantSlug"] = tenantSlug;
        ViewData["Title"] = $"API Docs — {tenantSlug}";
        return View();
    }

    [HttpGet("{tenantSlug}/manifest")]
    public async Task<IActionResult> Manifest(
        string tenantSlug, [FromQuery] string? credential, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(credential))
            return BadRequest(new { error = "Credential required" });

        var result = await _service.GetManifestAsync(tenantSlug, credential.Trim(), ct);
        if (!result.IsSuccess)
            return Unauthorized(new { error = result.Error });

        return Json(result.Value);
    }
}
