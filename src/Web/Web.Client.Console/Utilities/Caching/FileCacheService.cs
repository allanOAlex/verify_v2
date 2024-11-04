using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Web.Client.Console.Utilities.Caching;
internal sealed class FileCacheService : ICacheService
{
    private readonly string _cacheFilePath;

    public FileCacheService(string cacheFilePath)
    {
        _cacheFilePath = cacheFilePath;
            
    }

    public T? Get<T>(string key)
    {
        if (File.Exists(_cacheFilePath))
        {
            var json = File.ReadAllText(_cacheFilePath);
            var cache = MessagePackSerializer.Deserialize<Dictionary<string, T>>(Encoding.UTF8.GetBytes(json));
            if (cache != null && cache.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return default;
    }

    public void Remove<T>(string key)
    {
        if (File.Exists(_cacheFilePath))
        {
            var json = File.ReadAllText(_cacheFilePath);
            var cache = MessagePackSerializer.Deserialize<Dictionary<string, T>>(Encoding.UTF8.GetBytes(json));
            cache?.Remove(key);
            File.WriteAllText(_cacheFilePath, JsonSerializer.Serialize(cache));
        }
    }

    public void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration)
    {
        try
        {
            var cache = new Dictionary<string, T>();

            if (File.Exists(_cacheFilePath))
            {
                var json = File.ReadAllText(_cacheFilePath);
                cache = MessagePackSerializer.Deserialize<Dictionary<string, T>>(Encoding.UTF8.GetBytes(json)) ?? new Dictionary<string, T>();
                //cache = JsonSerializer.Deserialize<Dictionary<string, T>>(json) ?? new Dictionary<string, T>();
            }

            cache[key] = value;


            File.WriteAllText(_cacheFilePath, JsonSerializer.Serialize(cache));
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception)
        {

            throw;
        }
        
    }
}
