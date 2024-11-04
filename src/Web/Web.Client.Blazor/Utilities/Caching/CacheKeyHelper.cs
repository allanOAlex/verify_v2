namespace Web.Client.Blazor.Utilities.Caching;

internal sealed class CacheKeyHelper
{
    public static string GenerateCacheKey(string recipientBic, string recipientAccountNumber)
    {
        return $"{recipientBic}:{recipientAccountNumber}";
    }
}