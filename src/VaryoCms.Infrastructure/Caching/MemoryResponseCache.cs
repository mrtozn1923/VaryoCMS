using System.Collections.Concurrent;
using VaryoCms.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace VaryoCms.Infrastructure.Caching;

// IMemoryCache-backed response cache (shares the app's memory cache with the rate limiter;
// keys are namespaced with an "api:" prefix to avoid collisions).
// Tracks active keys in a ConcurrentDictionary so RemoveByPrefix can selectively invalidate
// entries after a write operation without scanning the entire memory cache.
public class MemoryResponseCache : IResponseCache
{
    private readonly IMemoryCache _cache;
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    public MemoryResponseCache(IMemoryCache cache) => _cache = cache;

    public bool TryGet<T>(string key, out T? value) where T : class
    {
        if (_cache.TryGetValue(key, out var cached) && cached is T typed)
        {
            value = typed;
            return true;
        }
        value = null;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan ttl) where T : class
    {
        _keys.TryAdd(key, 0);
        var entry = _cache.CreateEntry(key);
        entry.Value = value;
        entry.AbsoluteExpirationRelativeToNow = ttl;
        // Remove from tracking when the entry evicts naturally.
        entry.RegisterPostEvictionCallback((k, _, _, _) => _keys.TryRemove((string)k, out _));
        entry.Dispose(); // Commits the entry to the cache.
    }

    public void RemoveByPrefix(string prefix)
    {
        foreach (string key in _keys.Keys)
            if (key.StartsWith(prefix, StringComparison.Ordinal))
            {
                _cache.Remove(key);
                _keys.TryRemove(key, out _);
            }
    }
}
