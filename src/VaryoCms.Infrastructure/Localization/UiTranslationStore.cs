using System.Collections.Concurrent;
using VaryoCms.Application.Common;
using VaryoCms.Application.Interfaces;
using VaryoCms.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace VaryoCms.Infrastructure.Localization;

// Singleton cache over IUiTranslationRepository. The IStringLocalizer API is synchronous, so cache misses
// block on the async repository — acceptable because results are cached until an explicit Invalidate().
public class UiTranslationStore : IUiTranslationStore
{
    private const string CulturesKey = "uiloc:cultures";
    private const string CultureItemsKey = "uiloc:cultureitems";
    private const string DefaultKey = "uiloc:default";

    private readonly IUiTranslationRepository _repo;
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _cachedCultures = new();

    public UiTranslationStore(IUiTranslationRepository repo, IMemoryCache cache)
    {
        _repo = repo;
        _cache = cache;
    }

    public IReadOnlyDictionary<string, string> GetForCulture(string culture)
    {
        culture = Normalize(culture);
        return _cache.GetOrCreate("uiloc:c:" + culture, _ =>
        {
            _cachedCultures[culture] = 1;
            return _repo.GetAllForCultureAsync(culture).GetAwaiter().GetResult();
        })!;
    }

    public IReadOnlyCollection<string> ActiveCultures =>
        _cache.GetOrCreate(CulturesKey, _ => _repo.GetActiveCultureCodesAsync().GetAwaiter().GetResult())!;

    public IReadOnlyList<UiCultureItem> ActiveCultureItems =>
        _cache.GetOrCreate(CultureItemsKey, _ =>
            (IReadOnlyList<UiCultureItem>)_repo.GetCulturesAsync(activeOnly: true).GetAwaiter().GetResult()
                .Select(c => new UiCultureItem(c.Code, c.Name)).ToList())!;

    public string DefaultCulture =>
        _cache.GetOrCreate(DefaultKey, _ => _repo.GetDefaultCultureCodeAsync().GetAwaiter().GetResult())!;

    public bool IsActiveCulture(string code) =>
        ActiveCultures.Any(c => string.Equals(c, Normalize(code), StringComparison.OrdinalIgnoreCase));

    public void Invalidate()
    {
        _cache.Remove(CulturesKey);
        _cache.Remove(CultureItemsKey);
        _cache.Remove(DefaultKey);
        foreach (var c in _cachedCultures.Keys) _cache.Remove("uiloc:c:" + c);
        _cachedCultures.Clear();
    }

    private static string Normalize(string code) => (code ?? string.Empty).Trim().ToLowerInvariant();
}
