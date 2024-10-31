using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Utilities.Caching;
internal sealed class CacheKeyHelper
{
    public static string GenerateCacheKey(string recipientBic, string recipientAccountNumber)
    {
        return $"{recipientBic}:{recipientAccountNumber}";
    }
}
