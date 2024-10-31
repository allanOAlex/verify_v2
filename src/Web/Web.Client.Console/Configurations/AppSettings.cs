namespace Web.Client.Console.Configurations;

internal class AppSettings
{
    public string? ApiBaseUrl { get; set; }
    public int TimeoutSeconds { get; set; }
    public string? CacheFilePath { get; set; }
}
