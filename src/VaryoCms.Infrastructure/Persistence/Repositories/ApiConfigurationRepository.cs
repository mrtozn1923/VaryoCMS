using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ApiConfigurationRepository : BaseRepository, IApiConfigurationRepository
{
    private const string Columns =
        "id, tenant_id, content_type_id, is_enabled, is_public, auth_type, api_key, allow_filtering, allow_sorting, allow_pagination, rate_limit_per_min, cache_seconds, allow_read, allow_create, allow_update, allow_delete, created_at, updated_at";

    private readonly ITenantContext _tenantContext;

    public ApiConfigurationRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<ApiConfiguration>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM api_configurations WHERE tenant_id = @TenantId",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ApiConfiguration>(command);
        return rows.AsList();
    }

    public async Task<ApiConfiguration?> GetByContentTypeIdAsync(int contentTypeId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM api_configurations
               WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId",
            new { ContentTypeId = contentTypeId, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ApiConfiguration>(command);
    }

    public async Task<int> UpsertAsync(ApiConfiguration entity, CancellationToken ct = default)
    {
        // auth_type / api_key columns are dormant; auth is now via api_credentials.
        // Writes is_enabled, is_public, allow_* flags, rate limits. Never touches api_key.
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"MERGE api_configurations AS t
              USING (SELECT @TenantId AS tenant_id, @ContentTypeId AS content_type_id) AS s
              ON t.tenant_id = s.tenant_id AND t.content_type_id = s.content_type_id
              WHEN MATCHED THEN UPDATE SET
                  is_enabled = @IsEnabled, is_public = @IsPublic,
                  allow_filtering = @AllowFiltering, allow_sorting = @AllowSorting,
                  allow_pagination = @AllowPagination, rate_limit_per_min = @RateLimitPerMin,
                  cache_seconds = @CacheSeconds, allow_read = @AllowRead,
                  allow_create = @AllowCreate, allow_update = @AllowUpdate,
                  allow_delete = @AllowDelete, updated_at = GETUTCDATE()
              WHEN NOT MATCHED THEN INSERT
                  (tenant_id, content_type_id, is_enabled, is_public, allow_filtering, allow_sorting,
                   allow_pagination, rate_limit_per_min, cache_seconds,
                   allow_read, allow_create, allow_update, allow_delete)
                  VALUES (@TenantId, @ContentTypeId, @IsEnabled, @IsPublic, @AllowFiltering, @AllowSorting,
                   @AllowPagination, @RateLimitPerMin, @CacheSeconds,
                   @AllowRead, @AllowCreate, @AllowUpdate, @AllowDelete)
              OUTPUT INSERTED.id;",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.ContentTypeId,
                entity.IsEnabled,
                entity.IsPublic,
                entity.AllowFiltering,
                entity.AllowSorting,
                entity.AllowPagination,
                entity.RateLimitPerMin,
                entity.CacheSeconds,
                entity.AllowRead,
                entity.AllowCreate,
                entity.AllowUpdate,
                entity.AllowDelete
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<IReadOnlyList<ApiFieldVisibility>> GetFieldVisibilityAsync(
        int apiConfigId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, api_configuration_id, content_field_id, is_visible, response_key_alias
              FROM api_field_visibility
              WHERE api_configuration_id = @ConfigId",
            new { ConfigId = apiConfigId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ApiFieldVisibility>(command);
        return rows.AsList();
    }

    public async Task ReplaceFieldVisibilityAsync(
        int apiConfigId, IReadOnlyList<ApiFieldVisibility> rows, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        var deleteCmd = new CommandDefinition(
            "DELETE FROM api_field_visibility WHERE api_configuration_id = @ConfigId",
            new { ConfigId = apiConfigId }, transaction: tx, cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        foreach (var r in rows)
        {
            var insertCmd = new CommandDefinition(
                @"INSERT INTO api_field_visibility
                      (api_configuration_id, content_field_id, is_visible, response_key_alias)
                  VALUES (@ConfigId, @ContentFieldId, @IsVisible, @ResponseKeyAlias)",
                new
                {
                    ConfigId = apiConfigId,
                    r.ContentFieldId,
                    r.IsVisible,
                    r.ResponseKeyAlias
                },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(insertCmd);
        }

        await tx.CommitAsync(ct);
    }
}
