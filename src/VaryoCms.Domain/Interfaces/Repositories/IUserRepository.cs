using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Tenant scope is applied inside the implementation (via ITenantContext).
public interface IUserRepository
{
    Task<(IReadOnlyList<User> Items, int Total)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null, CancellationToken ct = default);
    Task<int> CreateAsync(User entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(User entity, CancellationToken ct = default);
    Task<bool> UpdatePasswordAsync(int id, string passwordHash, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default);
}
