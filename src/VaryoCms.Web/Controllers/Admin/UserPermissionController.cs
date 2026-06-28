using VaryoCms.Application.DTOs.User;
using VaryoCms.Application.Interfaces;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VaryoCms.Web.Controllers.Admin;

[Authorize(Roles = "TenantAdmin,SystemAdmin")]
[Route("admin/users/{userId:int}/permissions")]
public class UserPermissionController : Controller
{
    private readonly IUserService _service;

    public UserPermissionController(IUserService service) => _service = service;

    [HttpGet("")]
    public async Task<IActionResult> Index(int userId, CancellationToken ct)
    {
        var result = await _service.GetPermissionsAsync(userId, ct);
        if (!result.IsSuccess) return NotFound();
        var dto = result.Value!;
        return View(new UserPermissionsViewModel
        {
            UserId = dto.UserId,
            UserEmail = dto.UserEmail,
            Permissions = dto.Permissions.ToList()
        });
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(int userId, UserPermissionsViewModel vm, CancellationToken ct)
    {
        await _service.SavePermissionsAsync(userId,
            new SaveUserPermissionsRequest { Permissions = vm.Permissions ?? new() }, ct);
        return RedirectToAction(nameof(Index), new { userId });
    }
}
