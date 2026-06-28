using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

public interface ILoginCodeRepository
{
    // Inserts a new code (replaces any existing unused code for the same email+tenant).
    Task UpsertAsync(string email, string tenantType, int? tenantId, string code, DateTime expiresAt, CancellationToken ct = default);

    // Returns the most recent unused, non-expired code for the given identity.
    Task<LoginCode?> GetActiveAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default);

    Task<bool> IncrementAttemptsAsync(long id, CancellationToken ct = default);
    Task<bool> MarkUsedAsync(long id, CancellationToken ct = default);

    // Deletes all unused codes for the identity (called on expiry/lock-out redirect).
    Task DeleteByEmailAsync(string email, string tenantType, int? tenantId, CancellationToken ct = default);
}
