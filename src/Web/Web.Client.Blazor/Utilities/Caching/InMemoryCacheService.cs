using Microsoft.Extensions.Caching.Memory;

namespace Web.Client.Blazor.Utilities.Caching;

internal sealed class InMemoryCacheService : IMemCacheService
{

    private readonly IMemoryCache _memoryCache;

    public InMemoryCacheService(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }


    public T? Get<T>(string key)
    {
        return _memoryCache.TryGetValue(key, out T? value) ? value : default;
    }

    public void Remove<T>(string key)
    {
        _memoryCache.Remove(key);
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
    {
        _memoryCache.Set(key, value, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            SlidingExpiration = slidingExpiration
        });
    }
}
