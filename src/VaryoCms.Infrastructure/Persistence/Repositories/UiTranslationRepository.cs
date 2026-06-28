using Dapper;
using VaryoCms.Domain.Entities;
using VaryoCms.Domain.Interfaces.Repositories;

namespace VaryoCms.Infrastructure.Persistence.Repositories;

// Global (cross-tenant): ui_cultures / ui_translations have no tenant_id. No ITenantContext.
public class UiTranslationRepository : IUiTranslationRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public UiTranslationRepository(IDbConnectionFactory connectionFactory)
        => _connectionFactory = connectionFactory;

    public async Task<IReadOnlyDictionary<string, string>> GetAllForCultureAsync(
        string culture, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<(string ResourceKey, string Value)>(new CommandDefinition(
            "SELECT resource_key AS ResourceKey, value AS Value FROM ui_translations WHERE culture = @Culture",
            new { Culture = culture }, cancellationToken: ct));

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in rows) map[key] = value;
        return map;
    }

    public async Task<IReadOnlyList<string>> GetActiveCultureCodesAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<string>(new CommandDefinition(
            "SELECT code FROM ui_cultures WHERE is_active = 1 ORDER BY is_default DESC, code ASC",
            cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<string> GetDefaultCultureCodeAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var code = await conn.ExecuteScalarAsync<string?>(new CommandDefinition(
            "SELECT TOP 1 code FROM ui_cultures WHERE is_active = 1 ORDER BY is_default DESC, code ASC",
            cancellationToken: ct));
        return code ?? "tr";
    }

    // --- Management ---

    public async Task<IReadOnlyList<UiCulture>> GetCulturesAsync(bool activeOnly, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var sql = @"SELECT code, name, is_default AS IsDefault, is_active AS IsActive
                    FROM ui_cultures"
                  + (activeOnly ? " WHERE is_active = 1" : "")
                  + " ORDER BY is_default DESC, code ASC";
        var rows = await conn.QueryAsync<UiCulture>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<bool> CultureExistsAsync(string code, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(1) FROM ui_cultures WHERE code = @Code", new { Code = code }, cancellationToken: ct)) > 0;
    }

    public async Task AddCultureAsync(string code, string name, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            "INSERT INTO ui_cultures (code, name, is_default, is_active) VALUES (@Code, @Name, 0, 1)",
            new { Code = code, Name = name }, cancellationToken: ct));
    }

    public async Task<(IReadOnlyList<string> Keys, int Total)> GetKeysAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        var args = new { Search = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%",
                         Skip = (page - 1) * pageSize, Take = pageSize };

        var keys = (await conn.QueryAsync<string>(new CommandDefinition(
            @"SELECT DISTINCT resource_key FROM ui_translations
              WHERE (@Search IS NULL OR resource_key LIKE @Search)
              ORDER BY resource_key
              OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY",
            args, cancellationToken: ct))).AsList();

        int total = await conn.ExecuteScalarAsync<int>(new CommandDefinition(
            @"SELECT COUNT(DISTINCT resource_key) FROM ui_translations
              WHERE (@Search IS NULL OR resource_key LIKE @Search)",
            args, cancellationToken: ct));

        return (keys, total);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>> GetValuesForKeysAsync(
        IReadOnlyList<string> keys, CancellationToken ct = default)
    {
        var result = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        if (keys.Count == 0) return result;

        using var conn = _connectionFactory.CreateConnection();
        var rows = await conn.QueryAsync<(string ResourceKey, string Culture, string Value)>(new CommandDefinition(
            @"SELECT resource_key AS ResourceKey, culture AS Culture, value AS Value
              FROM ui_translations WHERE resource_key IN @Keys",
            new { Keys = keys }, cancellationToken: ct));

        foreach (var (key, culture, value) in rows)
        {
            if (!result.TryGetValue(key, out var inner))
            {
                inner = new Dictionary<string, string>(StringComparer.Ordinal);
                result[key] = inner;
            }
            ((Dictionary<string, string>)inner)[culture] = value;
        }
        return result;
    }

    public async Task UpsertAsync(string culture, string resourceKey, string value, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        await conn.ExecuteAsync(new CommandDefinition(
            @"MERGE ui_translations AS t
              USING (SELECT @Culture AS culture, @Key AS resource_key) AS s
              ON t.culture = s.culture AND t.resource_key = s.resource_key
              WHEN MATCHED THEN UPDATE SET value = @Value
              WHEN NOT MATCHED THEN INSERT (culture, resource_key, value) VALUES (@Culture, @Key, @Value);",
            new { Culture = culture, Key = resourceKey, Value = value }, cancellationToken: ct));
    }

    public async Task<int> BulkUpsertAsync(
        string culture, IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
    {
        int n = 0;
        foreach (var kv in values)
        {
            await UpsertAsync(culture, kv.Key, kv.Value, ct);
            n++;
        }
        return n;
    }
}
