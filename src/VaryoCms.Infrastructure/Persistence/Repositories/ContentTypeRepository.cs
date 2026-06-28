using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentTypeRepository : BaseRepository, IContentTypeRepository
{
    private readonly ITenantContext _tenantContext;

    public ContentTypeRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<ContentType?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, tenant_id, name, slug, description, icon, is_published, sort_order, parent_id,
                     created_at, updated_at, is_deleted
              FROM content_types
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<ContentType>(command);
    }

    public async Task<IReadOnlyList<ContentType>> GetAllAsync(CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, tenant_id, name, slug, description, icon, is_published, sort_order, parent_id,
                     created_at, updated_at, is_deleted
              FROM content_types
              WHERE tenant_id = @TenantId AND is_deleted = 0
              ORDER BY sort_order ASC, name ASC",
            new { TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<ContentType>(command);
        return rows.AsList();
    }

    public async Task<int> CreateAsync(ContentType entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO content_types (tenant_id, name, slug, description, icon, is_published, sort_order, parent_id)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @Name, @Slug, @Description, @Icon, @IsPublished, @SortOrder, @ParentId)",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.Name,
                entity.Slug,
                entity.Description,
                entity.Icon,
                entity.IsPublished,
                entity.SortOrder,
                entity.ParentId
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(ContentType entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_types
              SET name = @Name, slug = @Slug, description = @Description, icon = @Icon,
                  is_published = @IsPublished, sort_order = @SortOrder, parent_id = @ParentId,
                  updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new
            {
                entity.Id,
                TenantId = _tenantContext.TenantId,
                entity.Name,
                entity.Slug,
                entity.Description,
                entity.Icon,
                entity.IsPublished,
                entity.SortOrder,
                entity.ParentId
            },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE content_types
              SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SlugExistsAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1)
              FROM content_types
              WHERE slug = @Slug AND tenant_id = @TenantId AND is_deleted = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { Slug = slug, TenantId = _tenantContext.TenantId, ExcludeId = excludeId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }
}
