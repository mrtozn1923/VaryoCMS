using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Auth;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

// Cross-tenant SystemAdmin auth — the tenant-less counterpart of AuthService.
public class SystemAuthService : ISystemAuthService
{
    private readonly ISystemAdminRepository _admins;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUser;
    private readonly IStringLocalizer<SharedResource> _t;

    public SystemAuthService(
        ISystemAdminRepository admins, IPasswordHasher passwordHasher, ICurrentUserContext currentUser,
        IStringLocalizer<SharedResource> t)
    {
        _admins = admins;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _t = t;
    }

    public async Task<Result<AuthenticatedSystemAdminDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<AuthenticatedSystemAdminDto>.Failure(_t["Err.AuthInvalid"]);

        var admin = await _admins.GetByEmailAsync(email.Trim(), ct);
        if (admin is null || !admin.IsActive)
            return Result<AuthenticatedSystemAdminDto>.Failure(_t["Err.AuthInvalid"]);

        if (!_passwordHasher.Verify(password, admin.PasswordHash))
            return Result<AuthenticatedSystemAdminDto>.Failure(_t["Err.AuthInvalid"]);

        return Result<AuthenticatedSystemAdminDto>.Success(new AuthenticatedSystemAdminDto
        {
            Id = admin.Id,
            Email = admin.Email,
            FullName = admin.FullName
        });
    }

    public async Task<AuthenticatedSystemAdminDto?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var admin = await _admins.GetByEmailAsync(email.Trim(), ct);
        if (admin is null || !admin.IsActive) return null;
        return new AuthenticatedSystemAdminDto { Id = admin.Id, Email = admin.Email, FullName = admin.FullName };
    }

    public async Task<Result> ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not int adminId)
            return Result.Failure(_t["Err.NotAuthenticated"]);

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8 || newPassword.Length > 128)
            return Result.Failure(_t["Err.PasswordLengthRange"]);

        var admin = await _admins.GetByIdAsync(adminId, ct);
        if (admin is null)
            return Result.Failure(_t["Err.SystemAdminNotFound"]);

        if (!_passwordHasher.Verify(currentPassword, admin.PasswordHash))
            return Result.Failure(_t["Err.CurrentPasswordIncorrect"]);

        await _admins.UpdatePasswordAsync(adminId, _passwordHasher.Hash(newPassword), ct);
        return Result.Success();
    }
}
