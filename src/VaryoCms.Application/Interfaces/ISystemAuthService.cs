using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.Auth;

namespace VaryoCms.Application.Interfaces;

// Cross-tenant authentication for platform-owner SystemAdmins (system_admins table).
public interface ISystemAuthService
{
    Task<Result<AuthenticatedSystemAdminDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default);

    // Fetches the active system admin by email (no password check) — used after code verification.
    Task<AuthenticatedSystemAdminDto?> FindByEmailAsync(string email, CancellationToken ct = default);

    Task<Result> ChangePasswordAsync(
        string currentPassword, string newPassword, CancellationToken ct = default);
}
