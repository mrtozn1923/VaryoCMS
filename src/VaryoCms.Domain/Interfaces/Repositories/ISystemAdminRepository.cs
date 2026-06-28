using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Cross-tenant: system_admins is a root table, so this repository does NOT use ITenantContext.
public interface ISystemAdminRepository
{
    Task<SystemAdmin?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<SystemAdmin?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<bool> UpdatePasswordAsync(int id, string passwordHash, CancellationToken ct = default);
}
