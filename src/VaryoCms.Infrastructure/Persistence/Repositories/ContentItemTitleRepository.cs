using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class ContentItemTitleRepository : BaseRepository, IContentItemTitleRepository
{
    private readonly ITenantContext _tenantContext;

    public ContentItemTitleRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    private sealed record TitleRow(string? Title, bool IsActive);
    private sealed record LangRow(int ContentItemId, string LanguageCode, bool IsActive);

    public async Task<(string? Title, bool IsActive)?> GetTitleAsync(int itemId, string languageCode, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT title, is_active FROM content_item_titles
              WHERE content_item_id = @ItemId AND language_code = @Lang AND tenant_id = @TenantId",
            new { ItemId = itemId, Lang = languageCode.Trim(), TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        TitleRow? row = await conn.QueryFirstOrDefaultAsync<TitleRow>(command);
        if (row is null) return null;
        return (row.Title, row.IsActive);
    }

    public async Task SaveTitleAsync(int itemId, string languageCode, string? title, bool isActive, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        string lang = languageCode.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            var deleteCmd = new CommandDefinition(
                @"DELETE FROM content_item_titles
                  WHERE content_item_id = @ItemId AND language_code = @Lang AND tenant_id = @TenantId",
                new { ItemId = itemId, Lang = lang, TenantId = _tenantContext.TenantId },
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
              WHEN MATCHED THEN
                UPDATE SET title = @Title, is_active = @IsActive, updated_at = GETUTCDATE()
              WHEN NOT MATCHED THEN
                INSERT (tenant_id, content_item_id, language_code, title, is_active)
                VALUES (@TenantId, @ItemId, @Lang, @Title, @IsActive);",
            new { ItemId = itemId, Lang = lang, TenantId = _tenantContext.TenantId, Title = title.Trim(), IsActive = isActive },
            cancellationToken: ct);
        await conn.ExecuteAsync(mergeCmd);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<(string Code, bool IsActive)>>> GetFilledLanguagesAsync(
        IReadOnlyList<int> itemIds, CancellationToken ct = default)
    {
        if (itemIds.Count == 0) return new Dictionary<int, IReadOnlyList<(string Code, bool IsActive)>>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT content_item_id, language_code, is_active
              FROM content_item_titles
              WHERE content_item_id IN @Ids AND tenant_id = @TenantId",
            new { Ids = itemIds, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);

        IEnumerable<LangRow> rows = await conn.QueryAsync<LangRow>(command);
        return rows.GroupBy(r => r.ContentItemId)
                   .ToDictionary(
                       g => g.Key,
                       g => (IReadOnlyList<(string Code, bool IsActive)>)g
                           .Select(r => (r.LanguageCode.Trim(), r.IsActive))
                           .ToList());
    }
}
