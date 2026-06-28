using System.Data.Common;
using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Explicit-tenant write repository for the public API. Does NOT use ITenantContext.
// SQL mirrors ContentItemRepository / ContentFieldValueRepository / ContentRelationRepository /
// ContentItemTitleRepository but replaces _tenantContext.TenantId with explicit @TenantId.
public class PublicApiWriteRepository : BaseRepository, IPublicApiWriteRepository
{
    private const string FieldColumns =
        "id, tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json";

    public PublicApiWriteRepository(IDbConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<ContentField>> GetContentTypeFieldsAsync(
        int tenantId, int contentTypeId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {FieldColumns} FROM content_fields
               WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0
               ORDER BY sort_order ASC",
            new { ContentTypeId = contentTypeId, TenantId = tenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ContentField>(command);
        return rows.AsList();
    }

    public async Task<bool> SlugExistsAsync(
        int tenantId, int contentTypeId, string slug, int? excludeItemId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM content_items
              WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId
                AND slug = @Slug AND is_deleted = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { ContentTypeId = contentTypeId, TenantId = tenantId, Slug = slug, ExcludeId = excludeItemId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> InsertItemAsync(
        int tenantId, int contentTypeId, string? slug, string status,
        CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO content_items
                  (tenant_id, content_type_id, slug, status, published_at, created_at, updated_at, is_deleted)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @ContentTypeId, @Slug, @Status,
                      CASE WHEN @Status = 'published' THEN GETUTCDATE() ELSE NULL END,
                      GETUTCDATE(), GETUTCDATE(), 0)",
            new { TenantId = tenantId, ContentTypeId = contentTypeId, Slug = slug, Status = status },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateItemAsync(
        int tenantId, int itemId, string? slug, string status, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_items
              SET slug = @Slug, status = @Status,
                  published_at = CASE WHEN @Status = 'published' AND published_at IS NULL THEN GETUTCDATE() ELSE published_at END,
                  updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = itemId, TenantId = tenantId, Slug = slug, Status = status },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteItemAsync(int tenantId, int itemId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_items
              SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = itemId, TenantId = tenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<ContentItem?> GetItemByIdAsync(
        int tenantId, int contentTypeId, int itemId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, tenant_id, content_type_id, slug, status, created_at, updated_at
              FROM content_items
              WHERE id = @Id AND content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = itemId, ContentTypeId = contentTypeId, TenantId = tenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentItem>(command);
    }

    public async Task SaveValuesAsync(
        int tenantId, int itemId, IReadOnlyList<ContentFieldValue> values, CancellationToken ct = default)
    {
        if (values.Count == 0) return;

        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        foreach (var v in values)
        {
            var mergeCmd = new CommandDefinition(
                @"MERGE content_field_values AS t
                  USING (SELECT @ItemId AS content_item_id, @FieldId AS content_field_id, @Lang AS language_code) AS s
                  ON t.content_item_id = s.content_item_id
                     AND t.content_field_id = s.content_field_id
                     AND t.language_code = s.language_code
                  WHEN MATCHED THEN UPDATE SET
                      value_text = @ValueText, value_number = @ValueNumber, value_bool = @ValueBool,
                      value_date = @ValueDate, value_date_end = @ValueDateEnd, value_media_id = @ValueMediaId,
                      updated_at = GETUTCDATE()
                  WHEN NOT MATCHED THEN INSERT
                      (tenant_id, content_item_id, content_field_id, language_code,
                       value_text, value_number, value_bool, value_date, value_date_end, value_media_id,
                       created_at, updated_at)
                      VALUES (@TenantId, @ItemId, @FieldId, @Lang,
                       @ValueText, @ValueNumber, @ValueBool, @ValueDate, @ValueDateEnd, @ValueMediaId,
                       GETUTCDATE(), GETUTCDATE());",
                new
                {
                    TenantId = tenantId,
                    ItemId = itemId,
                    FieldId = v.ContentFieldId,
                    Lang = v.LanguageCode,
                    v.ValueText,
                    v.ValueNumber,
                    v.ValueBool,
                    v.ValueDate,
                    v.ValueDateEnd,
                    v.ValueMediaId
                },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(mergeCmd);
        }

        await tx.CommitAsync(ct);
    }

    public async Task ReplaceRelationsAsync(
        int tenantId, int itemId, int fieldId, IReadOnlyList<int> targetIds, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        var deleteCmd = new CommandDefinition(
            @"DELETE FROM content_field_relations
              WHERE source_item_id = @SourceItemId AND source_field_id = @SourceFieldId AND tenant_id = @TenantId",
            new { SourceItemId = itemId, SourceFieldId = fieldId, TenantId = tenantId },
            transaction: tx, cancellationToken: ct);
        await conn.ExecuteAsync(deleteCmd);

        for (int i = 0; i < targetIds.Count; i++)
        {
            var insertCmd = new CommandDefinition(
                @"INSERT INTO content_field_relations (tenant_id, source_item_id, source_field_id, target_item_id, sort_order)
                  VALUES (@TenantId, @SourceItemId, @SourceFieldId, @TargetItemId, @SortOrder)",
                new { TenantId = tenantId, SourceItemId = itemId, SourceFieldId = fieldId, TargetItemId = targetIds[i], SortOrder = i },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(insertCmd);
        }

        await tx.CommitAsync(ct);
    }

    public async Task SaveTitleAsync(
        int tenantId, int itemId, string languageCode, string? title, bool isActive, CancellationToken ct = default)
    {
        using var conn = CreateConnection();

        if (string.IsNullOrWhiteSpace(title))
        {
            var deleteCmd = new CommandDefinition(
                @"DELETE FROM content_item_titles
                  WHERE content_item_id = @ItemId AND language_code = @Lang AND tenant_id = @TenantId",
                new { ItemId = itemId, Lang = languageCode, TenantId = tenantId },
                cancellationToken: ct);
            await conn.ExecuteAsync(deleteCmd);
            return;
        }

        var mergeCmd = new CommandDefinition(
            @"MERGE content_item_titles AS target
              USING (SELECT @ItemId AS content_item_id, @Lang AS language_code, @TenantId AS tenant_id) AS source
                ON target.content_item_id = source.content_item_id
               AND target.language_code   = source.language_code
               AND target.tenant_id       = source.tenant_id
              WHEN MATCHED THEN UPDATE SET title = @Title, is_active = @IsActive, updated_at = GETUTCDATE()
              WHEN NOT MATCHED THEN INSERT (tenant_id, content_item_id, language_code, title, is_active, created_at, updated_at)
                                    VALUES (@TenantId, @ItemId, @Lang, @Title, @IsActive, GETUTCDATE(), GETUTCDATE());",
            new { ItemId = itemId, Lang = languageCode, TenantId = tenantId, Title = title.Trim(), IsActive = isActive },
            cancellationToken: ct);
        await conn.ExecuteAsync(mergeCmd);
    }
}
