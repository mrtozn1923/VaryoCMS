using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class MediaRepository : BaseRepository, IMediaRepository
{
    private const string Columns =
        "id, tenant_id, file_name, original_name, file_path, media_type, mime_type, file_size_bytes, width, height, duration_secs, alt_text, metadata_json, uploaded_by, created_at, updated_at, is_deleted";

    private readonly ITenantContext _tenantContext;

    public MediaRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<(IReadOnlyList<MediaAsset> Items, int Total)> GetPagedAsync(
        string? mediaType, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            TenantId = _tenantContext.TenantId,
            MediaType = mediaType,
            Skip = (page - 1) * pageSize,
            Take = pageSize
        };

        var listCmd = new CommandDefinition(
            $@"SELECT {Columns} FROM media_assets
               WHERE tenant_id = @TenantId AND is_deleted = 0
                 AND (@MediaType IS NULL OR media_type = @MediaType)
               ORDER BY created_at DESC
               OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<MediaAsset>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            @"SELECT COUNT(1) FROM media_assets
              WHERE tenant_id = @TenantId AND is_deleted = 0
                AND (@MediaType IS NULL OR media_type = @MediaType)",
            args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<MediaAsset?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM media_assets
               WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<MediaAsset>(command);
    }

    public async Task<int> CreateAsync(MediaAsset entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO media_assets
                  (tenant_id, file_name, original_name, file_path, media_type, mime_type,
                   file_size_bytes, width, height, duration_secs, alt_text, metadata_json, uploaded_by)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @FileName, @OriginalName, @FilePath, @MediaType, @MimeType,
                   @FileSizeBytes, @Width, @Height, @DurationSecs, @AltText, @MetadataJson, @UploadedBy)",
            new
            {
                TenantId = _tenantContext.TenantId,
                entity.FileName,
                entity.OriginalName,
                entity.FilePath,
                entity.MediaType,
                entity.MimeType,
                entity.FileSizeBytes,
                entity.Width,
                entity.Height,
                entity.DurationSecs,
                entity.AltText,
                entity.MetadataJson,
                entity.UploadedBy
            },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task UpdateFileAsync(
        int id, string fileName, string filePath, int width, int height, long sizeBytes, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE media_assets
              SET file_name = @FileName, file_path = @FilePath, width = @Width, height = @Height,
                  file_size_bytes = @SizeBytes, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new
            {
                Id = id, FileName = fileName, FilePath = filePath,
                Width = width, Height = height, SizeBytes = sizeBytes, TenantId = _tenantContext.TenantId
            },
            cancellationToken: ct);
        await conn.ExecuteAsync(command);
    }

    public async Task UpdateMetaAsync(int id, string originalName, string? altText, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE media_assets
              SET original_name = @OriginalName, alt_text = @AltText, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, OriginalName = originalName, AltText = altText, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        await conn.ExecuteAsync(command);
    }

    public async Task<IReadOnlyList<MediaAsset>> SearchAsync(
        string? query, string? mediaType, int limit, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT TOP (@Limit) {Columns} FROM media_assets
               WHERE tenant_id = @TenantId AND is_deleted = 0
                 AND (@MediaType IS NULL OR media_type = @MediaType)
                 AND (@Q IS NULL OR original_name LIKE @QLike OR file_name LIKE @QLike)
               ORDER BY created_at DESC",
            new
            {
                TenantId = _tenantContext.TenantId,
                MediaType = mediaType,
                Limit = limit,
                Q = string.IsNullOrWhiteSpace(query) ? null : query,
                QLike = string.IsNullOrWhiteSpace(query) ? null : $"%{query}%"
            },
            cancellationToken: ct);
        return (await conn.QueryAsync<MediaAsset>(command)).AsList();
    }

    public async Task<IReadOnlyList<MediaAsset>> GetByIdsAsync(IReadOnlyList<int> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return Array.Empty<MediaAsset>();
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM media_assets
               WHERE tenant_id = @TenantId AND is_deleted = 0 AND id IN @Ids",
            new { TenantId = _tenantContext.TenantId, Ids = ids },
            cancellationToken: ct);
        return (await conn.QueryAsync<MediaAsset>(command)).AsList();
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE media_assets SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }
}
