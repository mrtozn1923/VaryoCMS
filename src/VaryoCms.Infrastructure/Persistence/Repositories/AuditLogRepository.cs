using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : BaseRepository, IAuditLogRepository
{
    private readonly ITenantContext _tenantContext;

    public AuditLogRepository(IDbConnectionFactory factory, ITenantContext tenantContext)
        : base(factory) => _tenantContext = tenantContext;

    public async Task<long> InsertAsync(AuditLog entry, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var cmd = new CommandDefinition(
            @"INSERT INTO audit_logs
                (tenant_id, user_id, user_email, user_role, action, entity_type, entity_id,
                 content_type_id, entity_name, description, metadata_json, ip_address)
              OUTPUT INSERTED.id
              VALUES
                (@TenantId, @UserId, @UserEmail, @UserRole, @Action, @EntityType, @EntityId,
                 @ContentTypeId, @EntityName, @Description, @MetadataJson, @IpAddress)",
            new
            {
                entry.TenantId, entry.UserId, entry.UserEmail, entry.UserRole,
                entry.Action, entry.EntityType, entry.EntityId, entry.ContentTypeId,
                entry.EntityName, entry.Description, entry.MetadataJson, entry.IpAddress,
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<long>(cmd);
    }

    public async Task<(IReadOnlyList<AuditLog> Items, int Total)> GetPagedAsync(
        int page, int pageSize,
        string? action, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo,
        CancellationToken ct = default)
    {
        string? actionLike     = string.IsNullOrEmpty(action)     ? null : $"%{action}%";
        string? entityTypeLike = string.IsNullOrEmpty(entityType)  ? null : $"%{entityType}%";
        string where = BuildWhere(actionLike, entityTypeLike, contentTypeId, dateFrom, dateTo);
        using var conn = CreateConnection();

        int total = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM audit_logs WHERE tenant_id = @TenantId {where}",
                new { TenantId = _tenantContext.TenantId, action = actionLike, entityType = entityTypeLike, contentTypeId, dateFrom, dateTo },
                cancellationToken: ct));

        var items = await conn.QueryAsync<AuditLog>(
            new CommandDefinition(
                $@"SELECT id, tenant_id, user_id, user_email, user_role, action, entity_type, entity_id,
                          content_type_id, entity_name, description, metadata_json, ip_address, created_at
                   FROM audit_logs
                   WHERE tenant_id = @TenantId {where}
                   ORDER BY created_at DESC
                   OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY",
                new
                {
                    TenantId = _tenantContext.TenantId, action = actionLike, entityType = entityTypeLike, contentTypeId,
                    dateFrom, dateTo, Offset = (page - 1) * pageSize, PageSize = pageSize,
                },
                cancellationToken: ct));

        return (items.AsList(), total);
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentForContentTypeAsync(
        int contentTypeId, int take, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var items = await conn.QueryAsync<AuditLog>(
            new CommandDefinition(
                @"SELECT TOP (@Take) id, tenant_id, user_id, user_email, user_role, action,
                         entity_type, entity_id, content_type_id, entity_name, description, created_at
                  FROM audit_logs
                  WHERE tenant_id = @TenantId AND content_type_id = @ContentTypeId
                  ORDER BY created_at DESC",
                new { TenantId = _tenantContext.TenantId, ContentTypeId = contentTypeId, Take = take },
                cancellationToken: ct));
        return items.AsList();
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var items = await conn.QueryAsync<AuditLog>(
            new CommandDefinition(
                @"SELECT TOP (@Take) id, tenant_id, user_id, user_email, user_role, action,
                         entity_type, entity_id, content_type_id, entity_name, description, created_at
                  FROM audit_logs
                  WHERE tenant_id = @TenantId
                  ORDER BY created_at DESC",
                new { TenantId = _tenantContext.TenantId, Take = take },
                cancellationToken: ct));
        return items.AsList();
    }

    private static string BuildWhere(
        string? action, string? entityType, int? contentTypeId,
        DateTime? dateFrom, DateTime? dateTo)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(action))      parts.Add("AND action LIKE @Action");
        if (!string.IsNullOrEmpty(entityType))  parts.Add("AND entity_type LIKE @EntityType");
        if (contentTypeId.HasValue)             parts.Add("AND content_type_id = @ContentTypeId");
        if (dateFrom.HasValue)                  parts.Add("AND created_at >= @DateFrom");
        if (dateTo.HasValue)                    parts.Add("AND created_at < @DateTo");
        return string.Join(" ", parts);
    }
}
