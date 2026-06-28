using VaryoCms.Application.Common;

namespace VaryoCms.Application.Interfaces;

// Resolves a tenant by its subdomain slug. Implemented by Infrastructure (Dapper) — see Step 12.
// A dev-only stub is used until the real repository lands.
public interface ITenantStore
{
    Task<TenantInfo?> FindBySlugAsync(string slug, CancellationToken ct = default);

    // Resolve a tenant by id (used by impersonation to override the request's tenant context).
    Task<TenantInfo?> FindByIdAsync(int id, CancellationToken ct = default);
}
