using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Read-only, explicit-tenant repository for the public API. Does NOT use ITenantContext.
public class PublicApiRepository : BaseRepository, IPublicApiRepository
{
    private const string ItemColumns =
        "id, tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted";
    private const string ValueColumns =
        "id, tenant_id, content_item_id, content_field_id, language_code, value_text, value_number, value_bool, value_date, value_date_end, value_media_id, created_at, updated_at";

    // Whitelists (defense-in-depth; the service also validates).
    private static readonly HashSet<string> SortableItemColumns =
        new(StringComparer.OrdinalIgnoreCase) { "id", "created_at", "updated_at", "published_at", "status" };
    private static readonly HashSet<string> ValueColumnWhitelist =
        new(StringComparer.OrdinalIgnoreCase) { "value_text", "value_number", "value_bool", "value_date", "value_media_id" };

    public PublicApiRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<ApiConfiguration?> GetEnabledConfigAsync(
        int tenantId, string contentTypeSlug, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT c.id, c.tenant_id, c.content_type_id, c.is_enabled, c.is_public,
                     c.allow_filtering, c.allow_sorting, c.allow_pagination, c.rate_limit_per_min,
                     c.cache_seconds, c.allow_read, c.allow_create, c.allow_update, c.allow_delete,
                     c.created_at, c.updated_at
              FROM api_configurations c
              JOIN content_types ct ON ct.id = c.content_type_id
                   AND ct.tenant_id = c.tenant_id AND ct.is_deleted = 0
              WHERE c.tenant_id = @TenantId AND ct.slug = @Slug AND c.is_enabled = 1",
            new { TenantId = tenantId, Slug = contentTypeSlug },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ApiConfiguration>(command);
    }

    public async Task<IReadOnlyList<(ContentField Field, string? Alias)>> GetVisibleFieldsAsync(
        int apiConfigId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT cf.id, cf.tenant_id, cf.content_type_id, cf.name, cf.slug, cf.field_type,
                     cf.is_required, cf.is_localized, cf.sort_order, cf.options_json,
                     cf.created_at, cf.updated_at, cf.is_deleted,
                     v.response_key_alias AS Alias
              FROM api_field_visibility v
              JOIN content_fields cf ON cf.id = v.content_field_id AND cf.is_deleted = 0
              WHERE v.api_configuration_id = @ConfigId AND v.is_visible = 1
              ORDER BY cf.sort_order ASC",
            new { ConfigId = apiConfigId },
            cancellationToken: ct);

        var rows = await conn.QueryAsync<ContentField, string?, (ContentField, string?)>(
            command, (field, alias) => (field, alias), splitOn: "Alias");
        return rows.AsList();
    }

    public async Task<(IReadOnlyList<ContentItem> Items, int Total)> GetItemsAsync(
        ApiItemQuery query, CancellationToken ct = default)
    {
        var args = new DynamicParameters();
        args.Add("TenantId", query.TenantId);
        args.Add("ContentTypeId", query.ContentTypeId);
        args.Add("Status", query.Status);
        args.Add("Lang", query.Lang);

        // EAV equality filters → one EXISTS per filter (value parameterized; column whitelisted, field id is int).
        var whereBuilder = new System.Text.StringBuilder(
            "WHERE ci.tenant_id = @TenantId AND ci.content_type_id = @ContentTypeId AND ci.is_deleted = 0 " +
            "AND (@Status IS NULL OR ci.status = @Status) " +
            "AND EXISTS (SELECT 1 FROM content_item_titles cit " +
            "WHERE cit.content_item_id = ci.id AND cit.language_code = @Lang " +
            "AND cit.is_active = 1 AND cit.tenant_id = ci.tenant_id)");
        for (int i = 0; i < query.Filters.Count; i++)
        {
            var f = query.Filters[i];
            if (!ValueColumnWhitelist.Contains(f.Column)) continue;
            args.Add($"FVal{i}", f.Value);
            whereBuilder.Append(
                $" AND EXISTS (SELECT 1 FROM content_field_values fv{i} " +
                $"WHERE fv{i}.content_item_id = ci.id AND fv{i}.content_field_id = {f.FieldId} " +
                $"AND fv{i}.language_code IN (@Lang, 'all') AND fv{i}.{f.Column} = @FVal{i})");
        }
        string where = whereBuilder.ToString();

        string dir = query.SortDesc ? "DESC" : "ASC";
        string orderBy;
        if (query.SortFieldId is int sortFieldId
            && query.SortFieldColumn is string sortCol && ValueColumnWhitelist.Contains(sortCol))
        {
            // Sort by an EAV field value via a correlated subquery (prefers the requested language).
            orderBy =
                $"(SELECT TOP 1 sv.{sortCol} FROM content_field_values sv " +
                $"WHERE sv.content_item_id = ci.id AND sv.content_field_id = {sortFieldId} " +
                $"AND sv.language_code IN (@Lang, 'all') " +
                $"ORDER BY CASE WHEN sv.language_code = @Lang THEN 0 ELSE 1 END) {dir}, ci.id {dir}";
        }
        else
        {
            string itemCol = query.SortItemColumn is not null && SortableItemColumns.Contains(query.SortItemColumn)
                ? query.SortItemColumn : "created_at";
            orderBy = $"ci.{itemCol} {dir}";
        }

        using var conn = CreateConnection();

        args.Add("Skip", query.Skip);
        args.Add("Take", query.Take);
        // Only content_items is in the FROM (filters use EXISTS, sort uses a correlated subquery),
        // so unqualified ItemColumns are unambiguous.
        var listCmd = new CommandDefinition(
            $@"SELECT {ItemColumns}
               FROM content_items ci
               {where}
               ORDER BY {orderBy}
               OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<ContentItem>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            $@"SELECT COUNT(1) FROM content_items ci {where}", args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<ContentItem?> GetItemByIdAsync(
        int tenantId, int contentTypeId, int id, string lang, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {ItemColumns} FROM content_items ci
               WHERE ci.id = @Id AND ci.tenant_id = @TenantId AND ci.content_type_id = @ContentTypeId
                 AND ci.is_deleted = 0 AND ci.status = 'published'
                 AND EXISTS (SELECT 1 FROM content_item_titles cit
                             WHERE cit.content_item_id = ci.id AND cit.language_code = @Lang
                               AND cit.is_active = 1 AND cit.tenant_id = ci.tenant_id)",
            new { Id = id, TenantId = tenantId, ContentTypeId = contentTypeId, Lang = lang },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentItem>(command);
    }

    public async Task<ContentItem?> GetItemBySlugAsync(
        int tenantId, int contentTypeId, string slug, string lang, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {ItemColumns} FROM content_items ci
               WHERE ci.slug = @Slug AND ci.tenant_id = @TenantId AND ci.content_type_id = @ContentTypeId
                 AND ci.is_deleted = 0 AND ci.status = 'published'
                 AND EXISTS (SELECT 1 FROM content_item_titles cit
                             WHERE cit.content_item_id = ci.id AND cit.language_code = @Lang
                               AND cit.is_active = 1 AND cit.tenant_id = ci.tenant_id)",
            new { Slug = slug, TenantId = tenantId, ContentTypeId = contentTypeId, Lang = lang },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentItem>(command);
    }

    public async Task<IReadOnlyList<ContentFieldValue>> GetValuesAsync(
        int tenantId, IReadOnlyList<int> itemIds, string lang, CancellationToken ct = default)
    {
        if (itemIds.Count == 0) return Array.Empty<ContentFieldValue>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {ValueColumns} FROM content_field_values
               WHERE tenant_id = @TenantId AND content_item_id IN @Ids
                 AND language_code IN (@Lang, 'all')",
            new { TenantId = tenantId, Ids = itemIds, Lang = lang },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ContentFieldValue>(command);
        return rows.AsList();
    }

    public async Task<IReadOnlyList<(int SourceItemId, int SourceFieldId, int TargetItemId)>> GetRelationsAsync(
        int tenantId, IReadOnlyList<int> sourceItemIds, CancellationToken ct = default)
    {
        if (sourceItemIds.Count == 0) return Array.Empty<(int, int, int)>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT source_item_id AS SourceItemId, source_field_id AS SourceFieldId, target_item_id AS TargetItemId
              FROM content_field_relations
              WHERE tenant_id = @TenantId AND source_item_id IN @Ids
              ORDER BY source_item_id, source_field_id, sort_order",
            new { TenantId = tenantId, Ids = sourceItemIds },
            cancellationToken: ct);
        return (await conn.QueryAsync<(int SourceItemId, int SourceFieldId, int TargetItemId)>(command)).AsList();
    }

    public async Task<IReadOnlyDictionary<int, string>> GetDisplayValuesAsync(
        int tenantId, int targetContentTypeId, string? displayFieldSlug,
        IReadOnlyList<int> targetItemIds, string lang, CancellationToken ct = default)
    {
        if (targetItemIds.Count == 0) return new Dictionary<int, string>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT ci.id AS Id,
                     ISNULL(NULLIF((SELECT TOP 1 dv.value_text
                              FROM content_field_values dv
                              JOIN content_fields df ON df.id = dv.content_field_id
                                   AND df.content_type_id = @Ct AND df.slug = @DisplayField
                              WHERE dv.content_item_id = ci.id AND dv.tenant_id = @T
                                AND dv.language_code IN (@Lang, 'all')
                              ORDER BY CASE WHEN dv.language_code = @Lang THEN 0 ELSE 1 END), ''),
                          ISNULL(ci.slug, CONCAT('#', CAST(ci.id AS NVARCHAR(20))))) AS Display
              FROM content_items ci
              WHERE ci.tenant_id = @T AND ci.content_type_id = @Ct AND ci.id IN @Ids AND ci.is_deleted = 0",
            new { T = tenantId, Ct = targetContentTypeId, DisplayField = displayFieldSlug, Lang = lang, Ids = targetItemIds },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<(int Id, string Display)>(command);
        return rows.ToDictionary(r => r.Id, r => r.Display);
    }

    public async Task<IReadOnlyDictionary<int, string>> GetMediaUrlsAsync(
        int tenantId, IReadOnlyList<int> mediaIds, CancellationToken ct = default)
    {
        if (mediaIds.Count == 0) return new Dictionary<int, string>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id AS Id, file_path AS Url FROM media_assets
              WHERE tenant_id = @T AND id IN @Ids AND is_deleted = 0",
            new { T = tenantId, Ids = mediaIds },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<(int Id, string Url)>(command);
        return rows.ToDictionary(r => r.Id, r => r.Url);
    }

    public async Task<string?> GetApiKeyGrantHashAsync(
        int tenantId, int credentialId, int contentTypeId, CancellationToken ct = default)
    {
        // Returns the BCrypt hash for the given credential if it is active, not deleted,
        // covers contentTypeId, and belongs to the tenant. Returns null when no match.
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT c.api_key
              FROM api_credentials c
              JOIN api_credential_content_types j ON j.api_credential_id = c.id
              WHERE c.id = @CredentialId
                AND c.tenant_id = @TenantId
                AND c.auth_type = 'ApiKey'
                AND c.is_active = 1
                AND c.is_deleted = 0
                AND j.content_type_id = @ContentTypeId
                AND j.tenant_id = @TenantId",
            new { CredentialId = credentialId, TenantId = tenantId, ContentTypeId = contentTypeId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<string?>(command);
    }

    public async Task<IReadOnlyList<string>> GetCredentialContentTypeSlugsAsync(
        int tenantId, int credentialId, CancellationToken ct = default)
    {
        // Returns content type slugs granted by a JWT credential (for multi-CT JWT token issuance).
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT ct.slug
              FROM api_credential_content_types j
              JOIN content_types ct ON ct.id = j.content_type_id AND ct.is_deleted = 0
              WHERE j.api_credential_id = @CredentialId
                AND j.tenant_id = @TenantId",
            new { CredentialId = credentialId, TenantId = tenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<string>(command);
        return rows.AsList();
    }

    public async Task<string?> GetCredentialNameAsync(int tenantId, int credentialId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string?>(new CommandDefinition(
            @"SELECT name FROM api_credentials
              WHERE id = @CredentialId AND tenant_id = @TenantId AND is_deleted = 0",
            new { CredentialId = credentialId, TenantId = tenantId },
            cancellationToken: ct));
    }
}
