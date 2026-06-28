using Dapper;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Resolves a tenant by subdomain slug during request startup (before ITenantContext is set),
// so it deliberately does NOT depend on ITenantContext.
public class TenantStore : ITenantStore
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TenantStore(IDbConnectionFactory connectionFactory) => _connectionFactory = connectionFactory;

    public async Task<TenantInfo?> FindBySlugAsync(string slug, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"SELECT t.id,
                     t.slug,
                     t.name,
                     RTRIM(ISNULL((SELECT TOP 1 l.code
                             FROM languages l
                             WHERE l.tenant_id = t.id AND l.is_default = 1 AND l.is_active = 1),
                            'tr')) AS default_language_code   -- code is CHAR(5); trim padding
              FROM tenants t
              WHERE t.slug = @Slug AND t.is_active = 1 AND t.is_deleted = 0",
            new { Slug = slug },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<TenantInfo>(command);
    }

    public async Task<TenantInfo?> FindByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var command = new CommandDefinition(
            @"SELECT t.id,
                     t.slug,
                     t.name,
                     RTRIM(ISNULL((SELECT TOP 1 l.code
                             FROM languages l
                             WHERE l.tenant_id = t.id AND l.is_default = 1 AND l.is_active = 1),
                            'tr')) AS default_language_code   -- code is CHAR(5); trim padding
              FROM tenants t
              WHERE t.id = @Id AND t.is_active = 1 AND t.is_deleted = 0",
            new { Id = id },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<TenantInfo>(command);
    }
}
