using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface IUserPermissionRepository
{
    Task<IReadOnlyList<UserContentTypePermission>> GetByUserAsync(int userId, CancellationToken ct = default);
    // Replaces the user's permission rows in one transaction (delete-then-insert).
    Task ReplaceForUserAsync(
        int userId, IReadOnlyList<UserContentTypePermission> permissions, CancellationToken ct = default);
}
