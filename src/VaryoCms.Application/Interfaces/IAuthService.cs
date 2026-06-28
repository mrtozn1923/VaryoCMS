using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Auth;

namespace VaryoCms.Application.Interfaces;

public interface IAuthService
{
    // Validates email + password within the current tenant. Returns the identity on success.
    Task<Result<AuthenticatedUserDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default);

    // Fetches the active user by email (no password check) — used after code verification.
    Task<AuthenticatedUserDto?> FindByEmailAsync(string email, CancellationToken ct = default);

    // Changes the current (authenticated) user's password after verifying the current one.
    Task<Result> ChangePasswordAsync(string currentPassword, string newPassword, CancellationToken ct = default);
}
