using Dapper;
using VaryoCms.Domain.Interfaces.Repositories;
using VaryoCms.Infrastructure.Persistence;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public sealed class ApiDocRepository : IApiDocRepository
{
    private readonly IDbConnectionFactory _factory;

    public ApiDocRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<string?> GetApiKeyHashAsync(int tenantId, int credentialId, CancellationToken ct)
    {
        using var conn = _factory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            @"SELECT api_key FROM api_credentials
              WHERE id = @CredentialId AND tenant_id = @TenantId
                AND auth_type = 'ApiKey' AND is_active = 1 AND is_deleted = 0",
            new { CredentialId = credentialId, TenantId = tenantId },
            cancellationToken: ct));
    }

    public async Task<IReadOnlyList<ApiDocGroupRow>> GetGroupsAsync(
        int tenantId, IReadOnlyList<string> ctSlugs, CancellationToken ct)
    {
        if (ctSlugs.Count == 0) return [];

        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<ApiDocGroupRow>(new CommandDefinition(
            @"SELECT ct.name AS ContentTypeName, ct.slug AS ContentTypeSlug,
                     CAST(ac.allow_read   AS BIT) AS AllowRead,
                     CAST(ac.allow_create AS BIT) AS AllowCreate,
                     CAST(ac.allow_update AS BIT) AS AllowUpdate,
                     CAST(ac.allow_delete AS BIT) AS AllowDelete
              FROM content_types ct
              JOIN api_configurations ac ON ac.content_type_id = ct.id AND ac.is_enabled = 1
              WHERE ct.tenant_id = @TenantId
                AND ct.slug IN @CtSlugs
                AND ct.is_deleted = 0
              ORDER BY ct.sort_order",
            new { TenantId = tenantId, CtSlugs = ctSlugs },
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<IReadOnlyList<ApiDocFieldRow>> GetFieldsAsync(
        int tenantId, IReadOnlyList<string> ctSlugs, CancellationToken ct)
    {
        if (ctSlugs.Count == 0) return [];

        using var conn = _factory.CreateConnection();
        var rows = await conn.QueryAsync<ApiDocFieldRow>(new CommandDefinition(
            @"SELECT ct.slug AS ContentTypeSlug,
                     cf.name AS FieldName, cf.slug AS FieldSlug,
                     cf.field_type AS FieldType, cf.is_required AS IsRequired,
                     cf.is_localized AS IsLocalized,
                     afv.response_key_alias AS Alias
              FROM content_fields cf
              JOIN content_types ct ON ct.id = cf.content_type_id AND ct.is_deleted = 0
              JOIN api_configurations ac ON ac.content_type_id = ct.id AND ac.is_enabled = 1
              LEFT JOIN api_field_visibility afv
                     ON afv.api_configuration_id = ac.id AND afv.content_field_id = cf.id
              WHERE ct.tenant_id = @TenantId
                AND ct.slug IN @CtSlugs
                AND cf.is_deleted = 0
                AND COALESCE(afv.is_visible, 1) = 1
              ORDER BY ct.sort_order, cf.sort_order",
            new { TenantId = tenantId, CtSlugs = ctSlugs },
            cancellationToken: ct));
        return rows.AsList();
    }
}
