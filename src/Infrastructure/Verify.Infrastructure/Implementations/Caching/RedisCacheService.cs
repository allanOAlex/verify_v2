using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

using Verify.Application.Abstractions.IServices;

namespace Verify.Infrastructure.Implementations.Caching;
internal sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache cache;

    public RedisCacheService(IDistributedCache Cache)
    {
        cache = Cache;
    }


    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await cache.GetStringAsync(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
        catch (Exception)
        {

            throw;
        }

    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
    {
        try
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = absoluteExpiration,
                SlidingExpiration = slidingExpiration
            };

            var serializedValue = JsonSerializer.Serialize(value);
            await cache.SetStringAsync(key, serializedValue, options);
        }
        catch (Exception)
        {

            throw;
        }
    }
}
