using System.Data.Common;
using Dapper;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

public class DictionaryRepository : BaseRepository, IDictionaryRepository
{
    private const string Columns =
        "id, tenant_id, key_name, category, created_at, updated_at, is_deleted";

    private readonly ITenantContext _tenantContext;

    public DictionaryRepository(IDbConnectionFactory connectionFactory, ITenantContext tenantContext)
        : base(connectionFactory)
        => _tenantContext = tenantContext;

    public async Task<(IReadOnlyList<DictionaryEntry> Items, int Total)> GetPagedAsync(
        string? search, string? category, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var args = new
        {
            TenantId = _tenantContext.TenantId,
            Search = search,
            SearchLike = search is null ? null : $"%{search}%",
            Category = category,
            Skip = (page - 1) * pageSize,
            Take = pageSize
        };

        var listCmd = new CommandDefinition(
            $@"SELECT {Columns} FROM dictionary_entries
               WHERE tenant_id = @TenantId AND is_deleted = 0
                 AND (@Search IS NULL OR key_name LIKE @SearchLike)
                 AND (@Category IS NULL OR category = @Category)
               ORDER BY key_name ASC
               OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct);
        var items = (await conn.QueryAsync<DictionaryEntry>(listCmd)).AsList();

        var countCmd = new CommandDefinition(
            @"SELECT COUNT(1) FROM dictionary_entries
              WHERE tenant_id = @TenantId AND is_deleted = 0
                AND (@Search IS NULL OR key_name LIKE @SearchLike)
                AND (@Category IS NULL OR category = @Category)",
            args, cancellationToken: ct);
        int total = await conn.ExecuteScalarAsync<int>(countCmd);

        return (items, total);
    }

    public async Task<DictionaryEntry?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            $@"SELECT {Columns} FROM dictionary_entries
               WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.QueryFirstOrDefaultAsync<DictionaryEntry>(command);
    }

    public async Task<IReadOnlyList<DictionaryTranslation>> GetTranslationsAsync(
        int entryId, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT id, entry_id, language_code, value
              FROM dictionary_translations
              WHERE entry_id = @EntryId",
            new { EntryId = entryId },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<DictionaryTranslation>(command);
        return rows.AsList();
    }

    public async Task<IReadOnlyDictionary<int, int>> GetTranslatedCountsAsync(
        IReadOnlyList<int> entryIds, CancellationToken ct = default)
    {
        if (entryIds.Count == 0) return new Dictionary<int, int>();

        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT entry_id AS EntryId, COUNT(1) AS Cnt
              FROM dictionary_translations
              WHERE entry_id IN @Ids AND value IS NOT NULL AND LEN(LTRIM(RTRIM(value))) > 0
              GROUP BY entry_id",
            new { Ids = entryIds },
            cancellationToken: ct);
        var rows = await conn.QueryAsync<(int EntryId, int Cnt)>(command);
        return rows.ToDictionary(r => r.EntryId, r => r.Cnt);
    }

    public async Task<bool> KeyExistsAsync(string keyName, int? excludeId = null, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"SELECT COUNT(1) FROM dictionary_entries
              WHERE key_name = @KeyName AND tenant_id = @TenantId AND is_deleted = 0
                AND (@ExcludeId IS NULL OR id <> @ExcludeId)",
            new { KeyName = keyName, TenantId = _tenantContext.TenantId, ExcludeId = excludeId },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command) > 0;
    }

    public async Task<int> CreateAsync(DictionaryEntry entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"INSERT INTO dictionary_entries (tenant_id, key_name, category)
              OUTPUT INSERTED.id
              VALUES (@TenantId, @KeyName, @Category)",
            new { TenantId = _tenantContext.TenantId, entity.KeyName, entity.Category },
            cancellationToken: ct);
        return await conn.ExecuteScalarAsync<int>(command);
    }

    public async Task<bool> UpdateAsync(DictionaryEntry entity, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE dictionary_entries
              SET key_name = @KeyName, category = @Category, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { entity.Id, TenantId = _tenantContext.TenantId, entity.KeyName, entity.Category },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken ct = default)
    {
        using var conn = CreateConnection();
        var command = new CommandDefinition(
            @"UPDATE dictionary_entries SET is_deleted = 1, updated_at = GETUTCDATE()
              WHERE id = @Id AND tenant_id = @TenantId AND is_deleted = 0",
            new { Id = id, TenantId = _tenantContext.TenantId },
            cancellationToken: ct);
        return await conn.ExecuteAsync(command) > 0;
    }

    public async Task SaveTranslationsAsync(
        int entryId, IReadOnlyDictionary<string, string?> translations, CancellationToken ct = default)
    {
        if (translations.Count == 0) return;

        using var conn = (DbConnection)CreateConnection();
        await conn.OpenAsync(ct);
        using var tx = await conn.BeginTransactionAsync(ct);

        foreach (var (langRaw, valueRaw) in translations)
        {
            string lang = langRaw.Trim();
            string? value = valueRaw?.Trim();

            if (string.IsNullOrEmpty(value))
            {
                var del = new CommandDefinition(
                    @"DELETE FROM dictionary_translations
                      WHERE entry_id = @EntryId AND language_code = @Lang",
                    new { EntryId = entryId, Lang = lang }, transaction: tx, cancellationToken: ct);
                await conn.ExecuteAsync(del);
                continue;
            }

            var merge = new CommandDefinition(
                @"MERGE dictionary_translations AS t
                  USING (SELECT @EntryId AS entry_id, @Lang AS language_code) AS s
                  ON t.entry_id = s.entry_id AND t.language_code = s.language_code
                  WHEN MATCHED THEN UPDATE SET value = @Value
                  WHEN NOT MATCHED THEN INSERT (entry_id, language_code, value)
                      VALUES (@EntryId, @Lang, @Value);",
                new { EntryId = entryId, Lang = lang, Value = value },
                transaction: tx, cancellationToken: ct);
            await conn.ExecuteAsync(merge);
        }

        await tx.CommitAsync(ct);
    }
}
