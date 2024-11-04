using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;
using MessagePack;

namespace Web.Client.Blazor.Utilities.Caching;

internal sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }


    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _cache.GetAsync(key);
        return value == null ? default : MessagePackSerializer.Deserialize<T>(value);
    }

    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            SlidingExpiration = slidingExpiration
        };

        var serializedValue = MessagePackSerializer.Serialize(value);
        await _cache.SetAsync(key, serializedValue, options);
        //await _cache.SetStringAsync(key, Encoding.UTF8.GetString(serializedValue), options);
    }
}
