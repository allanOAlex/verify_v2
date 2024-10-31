using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Utilities.Caching;

public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration);
    void Remove<T>(string key);
}
