using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Auth;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserContext _currentUser;
    private readonly IStringLocalizer<SharedResource> _t;
    private readonly IAuditLogger _audit;

    public AuthService(
        IUserRepository users, IPasswordHasher passwordHasher, ICurrentUserContext currentUser,
        IStringLocalizer<SharedResource> t, IAuditLogger audit)
    {
        _users = users;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
        _t = t;
        _audit = audit;
    }

    public async Task<Result<AuthenticatedUserDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<AuthenticatedUserDto>.Failure(_t["Err.AuthInvalid"]);

        var user = await _users.GetByEmailAsync(email.Trim(), ct);
        if (user is null || !user.IsActive)
        {
            // Best-effort: audit logger needs tenant context — if unresolved, it will use TenantId=0.
            await _audit.LogAsync(AuditActions.LoginFailed, entityName: email, ct: ct);
            return Result<AuthenticatedUserDto>.Failure(_t["Err.AuthInvalid"]);
        }

        if (!_passwordHasher.Verify(password, user.PasswordHash))
        {
            await _audit.LogAsync(AuditActions.LoginFailed, entityName: email, ct: ct);
            return Result<AuthenticatedUserDto>.Failure(_t["Err.AuthInvalid"]);
        }

        return Result<AuthenticatedUserDto>.Success(new AuthenticatedUserDto
        {
            Id = user.Id,
            TenantId = user.TenantId,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role
        });
    }

    public async Task<AuthenticatedUserDto?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var user = await _users.GetByEmailAsync(email.Trim(), ct);
        if (user is null || !user.IsActive) return null;
        return new AuthenticatedUserDto
        {
            Id = user.Id, TenantId = user.TenantId,
            Email = user.Email, FullName = user.FullName, Role = user.Role
        };
    }

    public async Task<Result> ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken ct = default)
    {
        if (_currentUser.UserId is not int userId)
            return Result.Failure(_t["Err.NotAuthenticated"]);

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 8 || newPassword.Length > 128)
            return Result.Failure(_t["Err.PasswordLengthRange"]);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
            return Result.Failure(_t["Err.UserNotFound"]);

        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
            return Result.Failure(_t["Err.CurrentPasswordIncorrect"]);

        await _users.UpdatePasswordAsync(userId, _passwordHasher.Hash(newPassword), ct);
        await _audit.LogAsync(AuditActions.PasswordChanged, "User", userId, ct: ct);
        return Result.Success();
    }
}
