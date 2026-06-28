using VaryoCms.Domain.Entities;

namespace VaryoCms.Domain.Interfaces.Repositories;

// Cross-tenant: no ITenantContext dependency.
public interface ITenantEmailSettingsRepository
{
    Task<TenantEmailSettings?> GetByTenantIdAsync(int tenantId, CancellationToken ct = default);
    Task UpsertAsync(TenantEmailSettings settings, CancellationToken ct = default);
}
