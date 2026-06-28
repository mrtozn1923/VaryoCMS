namespace VaryoCms.Application.Interfaces;

// Caches public API responses for a TTL. Implemented by Infrastructure (in-memory).
public interface IResponseCache
{
    bool TryGet<T>(string key, out T? value) where T : class;
    void Set<T>(string key, T value, TimeSpan ttl) where T : class;
    // Removes all entries whose key starts with the given prefix (for post-write cache invalidation).
    void RemoveByPrefix(string prefix);
}
