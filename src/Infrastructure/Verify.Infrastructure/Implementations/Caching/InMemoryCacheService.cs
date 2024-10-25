using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Caching.Memory;
using Verify.Application.Abstractions.IServices;

namespace Verify.Infrastructure.Implementations.Caching;
internal class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache memoryCache;

    public InMemoryCacheService(IMemoryCache MemoryCache)
    {
        memoryCache = MemoryCache;
    }


    public T? Get<T>(string key)
    {
        return memoryCache.TryGetValue(key, out T? value) ? value : default;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        memoryCache.Remove(key);
    }

    public Task RemoveAsync(string key)
    {
        throw new NotImplementedException();
    }

    public void Set<T>(string key, T value, TimeSpan expiration)
    {
        memoryCache.Set(key, value, expiration);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
    {
        throw new NotImplementedException();
    }
}
