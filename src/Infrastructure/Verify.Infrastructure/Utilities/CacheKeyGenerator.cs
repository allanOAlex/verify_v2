using System.Collections.Concurrent;
using Verify.Application.Abstractions.IServices;

using Verify.Infrastructure.Configurations.Caching;

namespace Verify.Infrastructure.Utilities;
public static class CacheKeyGenerator
{
    private static readonly List<string> CacheKeys = [];
    private static CacheSetting? _cacheSettings;
    private static readonly object CacheKeysLock = new object(); // Lock object
    private static readonly ConcurrentDictionary<string, bool> ConcurrentCacheKeys = new ConcurrentDictionary<string, bool>();


    public static void Configure(CacheSetting cacheSettings) // Add a configuration method
    {
        _cacheSettings = cacheSettings;
    }

    public static string GenerateCacheKeyForOffsetPage(string entityName, int pageNumber)
    {
        var cacheKey = $"{_cacheSettings!.CacheKeyPrefix}{entityName}_OffsetPage_{pageNumber}";
        return GetOrCreateCacheKey(cacheKey);
    }

    public static string GenerateCacheKeyForCursorPage(string entityName, int cursor)
    {
        var cacheKey = $"{_cacheSettings!.CacheKeyPrefix}{entityName}_CursorPage_{cursor}";
        return GetOrCreateCacheKey(cacheKey);
    }

    public static string GenerateCacheKeyForEntity(string entityName, int id)
    {
        var cacheKey = $"{_cacheSettings!.CacheKeyPrefix}{entityName}_{id}";
        return GetOrCreateCacheKey(cacheKey);
    }

    private static string GetOrCreateCacheKey(string cacheKey)
    {
        if (!CacheKeys.Contains(cacheKey))
        {
            CacheKeys.Add(cacheKey);
        }

        return cacheKey;
    }

    private static string GetOrCreateCacheKeyWithLock(string cacheKey)
    {
        lock (CacheKeysLock)
        {
            if (!CacheKeys.Contains(cacheKey))
            {
                CacheKeys.Add(cacheKey);
            }
            return cacheKey;
        }
    }

    private static string AddCacheKeyIfNotExists(string cacheKey)
    {
        // TryAdd will return false if the key already exists
        ConcurrentCacheKeys.TryAdd(cacheKey, true);
        return cacheKey;
    }

    public static IEnumerable<string> GetCacheKeysForEntity(string entityName)
    {
        var prefixedEntityName = $"{_cacheSettings!.CacheKeyPrefix}{entityName}";
        return CacheKeys.Where(key => key.StartsWith(prefixedEntityName));
    }

    public static void InvalidateCacheKeysForEntity(string entityName, ICacheService cacheService)
    {
        var keysToInvalidate = GetCacheKeysForEntity(entityName).ToList();
        foreach (var key in keysToInvalidate)
        {
            cacheService.RemoveAsync(key);
            CacheKeys.Remove(key);
            ConcurrentCacheKeys.TryRemove(key, out _);
        }
    }
}

