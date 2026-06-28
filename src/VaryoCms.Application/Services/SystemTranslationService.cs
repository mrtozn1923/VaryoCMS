using System.Text.Json;
using VaryoCms.Application.Common;
using VaryoCms.Application.DTOs.System;
using VaryoCms.Application.Interfaces;
using VaryoCms.Application.Localization;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Localization;

namespace VaryoCms.Application.Services;

public class SystemTranslationService : ISystemTranslationService
{
    private readonly IUiTranslationRepository _repo;
    private readonly IUiTranslationStore _store;   // cache to invalidate after writes
    private readonly IStringLocalizer<SharedResource> _t;

    public SystemTranslationService(
        IUiTranslationRepository repo, IUiTranslationStore store, IStringLocalizer<SharedResource> t)
    {
        _repo = repo;
        _store = store;
        _t = t;
    }

    public async Task<SystemTranslationListDto> GetListAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 30 : pageSize;

        var cultures = await _repo.GetCulturesAsync(activeOnly: false, ct);
        var (keys, total) = await _repo.GetKeysAsync(search, page, pageSize, ct);
        var values = await _repo.GetValuesForKeysAsync(keys, ct);

        var keyDtos = keys.Select(k => new TranslationKeyDto
        {
            Key = k,
            Values = values.TryGetValue(k, out var v) ? v : new Dictionary<string, string>()
        }).ToList();

        return new SystemTranslationListDto
        {
            Cultures = cultures.Select(MapCulture).ToList(),
            Keys = keyDtos,
            Search = search,
            Page = page,
            PageSize = pageSize,
            Total = total,
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0
        };
    }

    public async Task<Result<TranslationKeyDto>> GetKeyAsync(string key, CancellationToken ct = default)
    {
        var values = await _repo.GetValuesForKeysAsync(new[] { key }, ct);
        if (!values.ContainsKey(key)) return Result<TranslationKeyDto>.Failure(_t["Msg.TranslationKeyNotFound"]);
        return Result<TranslationKeyDto>.Success(new TranslationKeyDto { Key = key, Values = values[key] });
    }

    public async Task<Result> SaveKeyAsync(
        string key, IReadOnlyDictionary<string, string> values, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return Result.Failure(_t["Msg.KeyRequired"]);
        foreach (var kv in values)
            await _repo.UpsertAsync(kv.Key, key, kv.Value ?? string.Empty, ct);
        _store.Invalidate();
        return Result.Success();
    }

    public async Task<IReadOnlyList<UiCultureDto>> GetCulturesAsync(CancellationToken ct = default)
        => (await _repo.GetCulturesAsync(activeOnly: false, ct)).Select(MapCulture).ToList();

    public async Task<Result> AddCultureAsync(string code, string name, CancellationToken ct = default)
    {
        code = (code ?? string.Empty).Trim().ToLowerInvariant();
        name = (name ?? string.Empty).Trim();
        if (!System.Text.RegularExpressions.Regex.IsMatch(code, "^[a-z]{2,5}$"))
            return Result.Failure(_t["Msg.CultureCodeInvalid"]);
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure(_t["Msg.NameRequired"]);
        if (await _repo.CultureExistsAsync(code, ct))
            return Result.Failure(_t["Msg.CultureExists", code]);

        await _repo.AddCultureAsync(code, name, ct);
        _store.Invalidate();
        return Result.Success();
    }

    public async Task<string> ExportAsync(string culture, CancellationToken ct = default)
    {
        var map = await _repo.GetAllForCultureAsync(culture.Trim().ToLowerInvariant(), ct);
        var ordered = map.OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        return JsonSerializer.Serialize(ordered, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task<Result<int>> ImportAsync(string culture, string json, CancellationToken ct = default)
    {
        culture = (culture ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(culture)) return Result<int>.Failure(_t["Msg.CultureRequired"]);
        if (!await _repo.CultureExistsAsync(culture, ct))
            return Result<int>.Failure(_t["Msg.CultureMissing", culture]);

        Dictionary<string, string>? map;
        try
        {
            map = JsonSerializer.Deserialize<Dictionary<string, string>>(json ?? string.Empty);
        }
        catch (JsonException)
        {
            return Result<int>.Failure(_t["Msg.InvalidJson"]);
        }
        if (map is null || map.Count == 0) return Result<int>.Failure(_t["Msg.NoEntries"]);

        int n = await _repo.BulkUpsertAsync(culture, map, ct);
        _store.Invalidate();
        return Result<int>.Success(n);
    }

    private static UiCultureDto MapCulture(Domain.Entities.UiCulture c) => new()
    {
        Code = c.Code,
        Name = c.Name,
        IsDefault = c.IsDefault,
        IsActive = c.IsActive
    };
}
