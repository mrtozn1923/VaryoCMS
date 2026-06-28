using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentRelationRepository : BaseRepository, IContentRelationRepository
{
    // Resolves an item's display text: the display field's value_text (preferring @Lang over 'all'),
    // else the slug, else "#id". @DisplayField = null safely falls through to the slug.
    private const string DisplayExpr =
        @"ISNULL(NULLIF((SELECT TOP 1 dv.value_text
                          FROM content_field_values dv
                          JOIN content_fields df ON df.id = dv.content_field_id
                               AND df.content_type_id = @Ct AND df.slug = @DisplayField
                          WHERE dv.content_item_id = ci.id AND dv.tenant_id = @T
                            AND dv.language_code IN (@Lang, 'all')
                          ORDER BY CASE WHEN dv.language_code = @Lang THEN 0 ELSE 1 END), ''),
                 ISNULL(ci.slug, CONCAT('#', CAST(ci.id AS NVARCHAR(20)))))";

    private readonly ITenantContext _tenantContext;

    public ContentRelationRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<int>> GetTargetIdsAsync(
        int sourceItemId, int sourceFieldId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT target_item_id FROM content_field_relations
              WHERE source_item_id = @SourceItemId AND source_field_id = @SourceFieldId AND tenant_id = @TenantId
              ORDER BY sort_order ASC",
            new { SourceItemId = sourceItemId, SourceFieldId = sourceFieldId, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return (await conn.QueryAsync<int>(command)).AsList();
    }

    public async Task ReplaceAsync(
        int sourceItemId, int sourceFieldId, IReadOnlyList<int> targetIds, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        await conn.ExecuteAsync(new CommandDefinition(
            @"DELETE FROM content_field_relations
              WHERE source_item_id = @SourceItemId AND source_field_id = @SourceFieldId AND tenant_id = @TenantId",
            new { SourceItemId = sourceItemId, SourceFieldId = sourceFieldId, TenantId = _tenantContext.TenantId },
            transaction: tx, cancellationToken: ct));

        for (int i = 0; i < targetIds.Count; i++)
        {
            await conn.ExecuteAsync(new CommandDefinition(
                @"INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
                  VALUES (@TenantId, @SourceItemId, @SourceFieldId, @TargetItemId, @SortOrder)",
                new
                {
                    TenantId = _tenantContext.TenantId,
                    SourceItemId = sourceItemId,
                    SourceFieldId = sourceFieldId,
                    TargetItemId = targetIds[i],
                    SortOrder = i
                },
                transaction: tx, cancellationToken: ct));
        }

        await tx.CommitAsync(ct);
    }

    public async Task<IReadOnlyList<(int Id, string Display)>> SearchTargetsAsync(
        int targetContentTypeId, string? displayFieldSlug, string? query, string lang, int limit, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            T = _tenantContext.TenantId,
            Ct = targetContentTypeId,
            DisplayField = displayFieldSlug,
            Lang = lang,
            Limit = limit,
            Q = string.IsNullOrWhiteSpace(query) ? null : query,
            QLike = string.IsNullOrWhiteSpace(query) ? null : $"%{query}%"
        };

        var command = new CommandDefinition(
            $@"SELECT TOP (@Limit) ci.id AS Id, {DisplayExpr} AS Display
               FROM content_items ci
               WHERE ci.tenant_id = @T AND ci.content_type_id = @Ct AND ci.is_deleted = 0 AND ci.status = 'published'
                 AND (@Q IS NULL
                      OR ci.slug LIKE @QLike
                      OR EXISTS (SELECT 1 FROM content_field_values fv
                                 JOIN content_fields ff ON ff.id = fv.content_field_id
                                      AND ff.content_type_id = @Ct AND ff.slug = @DisplayField
                                 WHERE fv.content_item_id = ci.id AND fv.tenant_id = @T AND fv.value_text LIKE @QLike))
               ORDER BY Display ASC",
            args, cancellationToken: ct);

        return (await conn.QueryAsync<(int Id, string Display)>(command)).AsList();
    }

    public async Task<IReadOnlyDictionary<int, string>> GetDisplayValuesAsync(
        int targetContentTypeId, string? displayFieldSlug, IReadOnlyList<int> itemIds, string lang, CancellationToken ct = default)
    {
        if (itemIds.Count == 0) return new Dictionary<int, string>();

        using var conn = CreateConnection();
        var args = new
        {
            T = _tenantContext.TenantId,
            Ct = targetContentTypeId,
            DisplayField = displayFieldSlug,
            Lang = lang,
            Ids = itemIds
        };

        var command = new CommandDefinition(
            $@"SELECT ci.id AS Id, {DisplayExpr} AS Display
               FROM content_items ci
               WHERE ci.tenant_id = @T AND ci.content_type_id = @Ct AND ci.id IN @Ids AND ci.is_deleted = 0",
            args, cancellationToken: ct);

        var rows = await conn.QueryAsync<(int Id, string Display)>(command);
        return rows.ToDictionary(r => r.Id, r => r.Display);
    }
}
