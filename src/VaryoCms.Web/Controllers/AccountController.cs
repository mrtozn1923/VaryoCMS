using System.Security.Claims;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Auth;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Web.Controllers;

[Authorize]
[Route("account")]
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly ILoginCodeService _loginCode;
    private readonly ITenantContext _tenant;
    private readonly IAuditLogger _audit;
    private readonly IStringLocalizer<SharedResource> _t;

    public AccountController(IAuthService auth, ILoginCodeService loginCode,
        ITenantContext tenant, IAuditLogger audit, IStringLocalizer<SharedResource> t)
    {
        _auth = auth; _loginCode = loginCode;
        _tenant = tenant; _audit = audit; _t = t;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login(string? returnUrl)
        => View(new LoginViewModel { ReturnUrl = returnUrl, TenantSlug = _tenant.TenantSlug }); // nosemgrep

    [AllowAnonymous]
    [HttpPost("login")]
    [ValidateAntiForgeryToken]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel vm, CancellationToken ct)
    {
        vm.TenantSlug = _tenant.TenantSlug;
        if (!ModelState.IsValid) return View(vm);

        var result = await _auth.ValidateCredentialsAsync(vm.Email, vm.Password, ct);
        if (!result.IsSuccess) { ModelState.AddModelError(string.Empty, result.Error!); return View(vm); }

        if (await _loginCode.IsTenantEnabledAsync(_tenant.TenantId, ct))
        {
            try { await _loginCode.SendCodeAsync(vm.Email, "tenant", _tenant.TenantId, ct); }
            catch { ModelState.AddModelError(string.Empty, _t["Login.EmailSendError"]); return View(vm); }

            await _audit.LogAsync(AuditActions.LoginCodeSent, "User", result.Value!.Id,
                entityName: vm.Email, userEmailOverride: vm.Email, userIdOverride: result.Value.Id, ct: ct);

            TempData["PendingEmail"] = vm.Email;
            TempData["RememberMe"]   = vm.RememberMe;
            TempData["ReturnUrl"]    = vm.ReturnUrl;
            return RedirectToAction(nameof(VerifyCode));
        }

        await SignInAsync(result.Value!, vm.RememberMe, ct);
        return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl!) : RedirectToAction("Index", "Home");
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

        var verify = await _loginCode.VerifyAsync(vm.Email, "tenant", _tenant.TenantId, vm.Code, ct);
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
            // "InvalidCode:{remaining}"
            int remaining = int.TryParse(verify.Error?.Split(':').ElementAtOrDefault(1), out int r) ? r : 0;
            ModelState.AddModelError(string.Empty, string.Format(_t["Login.InvalidCode"].Value, remaining));
            await _audit.LogAsync(AuditActions.LoginCodeFailed, entityName: vm.Email,
                userEmailOverride: vm.Email, ct: ct);
            return View(vm);
        }

        var user = await _auth.FindByEmailAsync(vm.Email, ct);
        if (user is null) return RedirectToAction(nameof(Login));

        await _audit.LogAsync(AuditActions.LoginCodeVerified, "User", user.Id,
            entityName: vm.Email, userEmailOverride: vm.Email, userIdOverride: user.Id, ct: ct);
        await SignInAsync(user, vm.RememberMe, ct);
        return Url.IsLocalUrl(vm.ReturnUrl) ? LocalRedirect(vm.ReturnUrl!) : RedirectToAction("Index", "Home");
    }

    [HttpPost("logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        await _audit.LogAsync(AuditActions.Logout, ct: ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] [HttpGet("access-denied")]
    public IActionResult AccessDenied() => View();

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

    private async Task SignInAsync(AuthenticatedUserDto user, bool rememberMe, CancellationToken ct)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("TenantId", user.TenantId.ToString()),
            new("FullName", user.FullName ?? user.Email)
        };
        var props = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = rememberMe ? DateTimeOffset.UtcNow.AddDays(30) : null
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), props);
        await _audit.LogAsync(AuditActions.LoginSuccess, "User", user.Id,
            entityName: user.Email, userEmailOverride: user.Email, userIdOverride: user.Id, ct: ct);
    }
}
