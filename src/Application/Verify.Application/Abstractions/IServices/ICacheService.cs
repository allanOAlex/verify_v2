using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Abstractions.IServices;
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan absoluteExpiration, TimeSpan slidingExpiration);
    Task RemoveAsync(string key);
}
