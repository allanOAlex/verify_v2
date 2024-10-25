using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verify.Application.Abstractions.IServices;

using Verify.Infrastructure.Configurations.Caching;

namespace Verify.Infrastructure.Utilities;
public static class CacheKeyGenerator
{
    private static readonly List<string> CacheKeys = [];
    private static CacheSetting? cacheSettings;
    private static readonly object CacheKeysLock = new object(); // Lock object
    private static readonly ConcurrentDictionary<string, bool> ConcurrentCacheKeys = new ConcurrentDictionary<string, bool>();


    public static void Configure(CacheSetting CacheSettings) // Add a configuration method
    {
        cacheSettings = CacheSettings;
    }

    public static string GenerateCacheKeyForOffsetPage(string entityName, int pageNumber)
    {
        var cacheKey = $"{cacheSettings!.CacheKeyPrefix}{entityName}_OffsetPage_{pageNumber}";
        return GetOrCreateCacheKey(cacheKey);
    }

    public static string GenerateCacheKeyForCursorPage(string entityName, int cursor)
    {
        var cacheKey = $"{cacheSettings!.CacheKeyPrefix}{entityName}_CursorPage_{cursor}";
        return GetOrCreateCacheKey(cacheKey);
    }

    public static string GenerateCacheKeyForEntity(string entityName, int Id)
    {
        var cacheKey = $"{cacheSettings!.CacheKeyPrefix}{entityName}_{Id}";
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
        var prefixedEntityName = $"{cacheSettings!.CacheKeyPrefix}{entityName}";
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

