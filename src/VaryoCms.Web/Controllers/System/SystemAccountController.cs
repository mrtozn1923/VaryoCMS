using System.Security.Claims;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Enums;
using VaryoCms.Web.ViewModels;
using VaryoCms.Web.ViewModels.System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using VaryoCms.Application.Localization;
using VaryoCms.Application.DTOs.Auth;

namespace VaryoCms.Web.Controllers.System;

// Cross-tenant platform-owner auth. Separate from AccountController (which is tenant-scoped).
[Authorize(Roles = nameof(UserRole.SystemAdmin))]
[Route("system")]
public class SystemAccountController : Controller
{
    private readonly ISystemAuthService _auth;
    private readonly ILoginCodeService _loginCode;
    private readonly IStringLocalizer<SharedResource> _t;

    public SystemAccountController(ISystemAuthService auth, ILoginCodeService loginCode,
        IStringLocalizer<SharedResource> t)
    {
        _auth = auth; _loginCode = loginCode; _t = t;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl) => View(new SystemLoginViewModel { ReturnUrl = returnUrl }); // nosemgrep: csharp.dotnet.security.audit.mass-assignment.mass-assignment

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(SystemLoginViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var result = await _auth.ValidateCredentialsAsync(vm.Email, vm.Password, ct);
        if (!result.IsSuccess) { ModelState.AddModelError(string.Empty, result.Error!); return View(vm); }

        if (_loginCode.IsSystemEnabled)
        {
            try { await _loginCode.SendCodeAsync(vm.Email, "system", null, ct); }
            catch { ModelState.AddModelError(string.Empty, _t["Login.EmailSendError"]); return View(vm); }

            TempData["PendingEmail"] = vm.Email;
            TempData["RememberMe"]   = vm.RememberMe;
            TempData["ReturnUrl"]    = vm.ReturnUrl;
            return RedirectToAction(nameof(VerifyCode));
        }

        await SignInAsync(result.Value!, vm.RememberMe);
        return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl!) : Redirect("/system");
    }

    [AllowAnonymous] [HttpGet("verify-code")]
    public IActionResult VerifyCode()
    {
        if (TempData["PendingEmail"] is not string email) return RedirectToAction(nameof(Login));
        return View(new VerifyCodeViewModel
        {
            Email = email,
            RememberMe = TempData["RememberMe"] is bool r && r,
            ReturnUrl = TempData["ReturnUrl"] as string
        });
    }

    [AllowAnonymous]
    [HttpPost("verify-code")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyCode(VerifyCodeViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);

        var verify = await _loginCode.VerifyAsync(vm.Email, "system", null, vm.Code, ct);
        if (!verify.IsSuccess)
        {
            if (verify.Error is "ExpiredCode" or "MaxAttemptsExceeded" or "NotFound")
            {
                string msgKey = verify.Error == "ExpiredCode" ? "Login.ExpiredCode"
                    : verify.Error == "MaxAttemptsExceeded" ? "Login.MaxAttemptsExceeded"
                    : "Login.CodeNotFound";
                TempData["LoginError"] = _t[msgKey].Value;
                return RedirectToAction(nameof(Login));
            }
            int remaining = int.TryParse(verify.Error?.Split(':').ElementAtOrDefault(1), out int r) ? r : 0;
            ModelState.AddModelError(string.Empty, string.Format(_t["Login.InvalidCode"].Value, remaining));
            return View(vm);
        }

        var admin = await _auth.FindByEmailAsync(vm.Email, ct);
        if (admin is null) return RedirectToAction(nameof(Login));

        await SignInAsync(admin, vm.RememberMe);
        return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl!) : Redirect("/system");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet("change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost("change-password")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(vm);
        var result = await _auth.ChangePasswordAsync(vm.CurrentPassword, vm.NewPassword, ct);
        if (!result.IsSuccess) { ModelState.AddModelError(string.Empty, result.Error!); return View(vm); }
        TempData["PasswordChanged"] = true;
        return RedirectToAction(nameof(ChangePassword));
    }

    private async Task SignInAsync(AuthenticatedSystemAdminDto admin, bool rememberMe)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new(ClaimTypes.Name, admin.Email),
            new(ClaimTypes.Email, admin.Email),
            new(ClaimTypes.Role, nameof(UserRole.SystemAdmin)),
            new("FullName", admin.FullName ?? admin.Email)
        };
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), props);
    }
}
