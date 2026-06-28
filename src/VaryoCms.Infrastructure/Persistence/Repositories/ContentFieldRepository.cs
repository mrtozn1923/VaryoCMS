using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentFieldRepository : BaseRepository, IContentFieldRepository
{
    private const string Columns =
        "id, tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json, created_at, updated_at, is_deleted";

    private readonly ITenantContext _tenantContext;

    public ContentFieldRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<IReadOnlyList<ContentField>> GetByContentTypeAsync(int contentTypeId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM content_fields
               WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0
               ORDER BY sort_order ASC, id ASC",
            new { ContentTypeId = contentTypeId, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ContentField>(command);
        return rows.AsList();
    }

    public async Task<ContentField?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM content_fields
               WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentField>(command);
    }

    public async Task<int> CreateAsync(ContentField entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        // sort_order = current max within the content type + 1 (append to the end), computed atomically.
        var command = new CommandDefinition(
            @"INSERT INTO content_fields
                  (tenant_id, content_type_id, name, slug, field_type, is_required, is_localized, sort_order, options_json)
              OUTPUT INSERTED.id
              SELECT @TenantId, @ContentTypeId, @Name, @Slug, @FieldType, @IsRequired, @IsLocalized,
                     ISNULL((SELECT MAX(sort_order) FROM content_fields
                             WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0), -1) + 1,
                     @OptionsJson",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.ContentTypeId,
                entity.Name,
                entity.Slug,
                // Enum -> name string: Dapper skips custom handlers for enum params on write,
                // so pass the name explicitly to match the NVARCHAR column.
                FieldType = entity.FieldType.ToString(),
                entity.IsRequired,
                entity.IsLocalized,
                entity.OptionsJson
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(ContentField entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_fields
              SET name = @Name, slug = @Slug, field_type = @FieldType, is_required = @IsRequired,
                  is_localized = @IsLocalized, options_json = @OptionsJson, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new
            {
                entity.Id,
                TenantId = _tenantContext.TenantId,
                entity.Name,
                entity.Slug,
                FieldType = entity.FieldType.ToString(),   // enum -> name string (see CreateAsync)
                entity.IsRequired,
                entity.IsLocalized,
                entity.OptionsJson
            },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_fields SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SlugExistsAsync(int contentTypeId, string slug, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM content_fields
              WHERE content_type_id = @ContentTypeId AND slug = @Slug AND tenant_id = @TenantId AND is_deleted = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { ContentTypeId = contentTypeId, Slug = slug, TenantId = _tenantContext.TenantId, ExcludeId = excludeId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> ReorderAsync(int contentTypeId, IReadOnlyList<int> orderedFieldIds, CancellationToken ct = default)
    {
        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        int updated = 0;
        for (int i = 0; i < orderedFieldIds.Count; i++)
        {
            var command = new CommandDefinition(
                @"UPDATE content_fields SET sort_order = @SortOrder, updated_at = GETUTCDATE()
                  WHERE id = @Id AND content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0",
                new { SortOrder = i, Id = orderedFieldIds[i], ContentTypeId = contentTypeId, TenantId = _tenantContext.TenantId },
                transaction: tx,
                cancellationToken: ct);
            updated += await conn.ExecuteAsync(command);
        }

        await tx.CommitAsync(ct);
        return updated;
    }
}
