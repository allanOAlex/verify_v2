
namespace Web.Client.Blazor.Utilities.Caching;

public interface IMemCacheService
{

    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration);
    void Remove<T>(string key);
}
