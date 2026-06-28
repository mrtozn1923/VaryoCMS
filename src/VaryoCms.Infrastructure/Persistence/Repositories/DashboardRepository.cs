using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class DashboardRepository : BaseRepository, IDashboardRepository
{
    private readonly ITenantContext _tenantContext;

    public DashboardRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory) => _tenantContext = tenantContext;

    public async Task<DashboardCounts> GetCountsAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var result = await conn.QueryFirstOrDefaultAsync<DashboardCounts>(
            new CommandDefinition(
                @"SELECT
                    (SELECT COUNT(1) FROM content_types   WHERE tenant_id = @TenantId AND is_deleted = 0) AS ContentTypes,
                    (SELECT COUNT(1) FROM content_items   WHERE tenant_id = @TenantId AND is_deleted = 0) AS ContentItems,
                    (SELECT COUNT(1) FROM media_assets    WHERE tenant_id = @TenantId AND is_deleted = 0) AS MediaAssets,
                    (SELECT COUNT(1) FROM users           WHERE tenant_id = @TenantId AND is_deleted = 0) AS Users,
                    (SELECT COUNT(1) FROM languages       WHERE tenant_id = @TenantId AND is_active  = 1) AS Languages",
                new { TenantId = _tenantContext.TenantId },
                cancellationToken: ct));

        return result ?? new DashboardCounts(0, 0, 0, 0, 0);
    }
}
