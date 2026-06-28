using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentItemRepository : BaseRepository, IContentItemRepository
{
    private const string Columns =
        "id, tenant_id, content_type_id, slug, status, created_by, updated_by, published_at, created_at, updated_at, is_deleted";

    private readonly ITenantContext _tenantContext;

    public ContentItemRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<(IReadOnlyList<ContentItem> Items, int Total)> GetPagedAsync(
        int contentTypeId, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            ContentTypeId = contentTypeId,
            TenantId = _tenantContext.TenantId,
            Skip = (page - 1) * pageSize,
            Take = pageSize
        };

        var listCmd = new CommandDefinition(
            $@"SELECT {Columns} FROM content_items
               WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0
               ORDER BY updated_at DESC
               OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<ContentItem>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            @"SELECT COUNT(1) FROM content_items
              WHERE content_type_id = @ContentTypeId AND tenant_id = @TenantId AND is_deleted = 0",
            args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<(IReadOnlyList<ContentItemListRow> Rows, int Total)> GetPagedListAsync(
        int contentTypeId, string languageCode, int page, int pageSize,
        string? searchQuery = null, string? statusFilter = null, string? languageFilter = null,
        CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            ContentTypeId = contentTypeId,
            TenantId = _tenantContext.TenantId,
            Lang = languageCode.Trim(),
            Skip = (page - 1) * pageSize,
            Take = pageSize,
            Q = string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim(),
            StatusFilter = string.IsNullOrWhiteSpace(statusFilter) ? null : statusFilter.Trim(),
            LanguageFilter = string.IsNullOrWhiteSpace(languageFilter) ? null : languageFilter.Trim()
        };

        const string filterWhere = @"
                AND (@Q IS NULL
                     OR ci.slug LIKE '%' + @Q + '%'
                     OR EXISTS (SELECT 1 FROM content_item_titles cit2
                                WHERE cit2.content_item_id = ci.id
                                  AND cit2.language_code   = @Lang
                                  AND cit2.title LIKE '%' + @Q + '%'
                                  AND cit2.tenant_id       = ci.tenant_id))
                AND (@StatusFilter IS NULL OR ci.status = @StatusFilter)
                AND (@LanguageFilter IS NULL OR EXISTS (
                     SELECT 1 FROM content_item_titles cit3
                     WHERE cit3.content_item_id = ci.id
                       AND cit3.language_code   = @LanguageFilter
                       AND cit3.is_active        = 1
                       AND cit3.tenant_id        = ci.tenant_id))";

        var listCmd = new CommandDefinition(
            $@"SELECT ci.id, ci.slug, ci.status, ci.created_at, ci.updated_at,
                     t.title,
                     COALESCE(cu.full_name, cu.email) AS created_by_name,
                     COALESCE(uu.full_name, uu.email) AS updated_by_name
              FROM content_items ci
              LEFT JOIN content_item_titles t
                     ON t.content_item_id = ci.id
                    AND t.language_code   = @Lang
                    AND t.tenant_id       = ci.tenant_id
              LEFT JOIN users cu ON cu.id = ci.created_by
              LEFT JOIN users uu ON uu.id = ci.updated_by
              WHERE ci.content_type_id = @ContentTypeId
                AND ci.tenant_id       = @TenantId
                AND ci.is_deleted      = 0
                {filterWhere}
              ORDER BY ci.updated_at DESC
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var rows = (await conn.QueryAsync<ContentItemListRow>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            $@"SELECT COUNT(1) FROM content_items ci
              WHERE ci.content_type_id = @ContentTypeId
                AND ci.tenant_id       = @TenantId
                AND ci.is_deleted      = 0
                {filterWhere}",
            args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (rows, total);
    }

    public async Task<bool> SlugExistsAsync(
        int contentTypeId, string slug, int? excludeItemId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM content_items
              WHERE content_type_id = @ContentTypeId
                AND tenant_id       = @TenantId
                AND slug            = @Slug
                AND is_deleted      = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new
            {
                ContentTypeId = contentTypeId,
                TenantId = _tenantContext.TenantId,
                Slug = slug,
                ExcludeId = excludeItemId
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<ContentItem?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM content_items
               WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentItem>(command);
    }

    public async Task<int> CreateAsync(ContentItem entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO content_items (tenant_id, content_type_id, slug, status, created_by, updated_by, published_at)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @ContentTypeId, @Slug, @Status, @CreatedBy, @UpdatedBy, @PublishedAt)",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.ContentTypeId,
                entity.Slug,
                entity.Status,
                entity.CreatedBy,
                entity.UpdatedBy,
                entity.PublishedAt
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(ContentItem entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_items
              SET slug = @Slug, status = @Status, updated_by = @UpdatedBy,
                  published_at = @PublishedAt, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new
            {
                entity.Id,
                TenantId = _tenantContext.TenantId,
                entity.Slug,
                entity.Status,
                entity.UpdatedBy,
                entity.PublishedAt
            },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_items SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }
}
