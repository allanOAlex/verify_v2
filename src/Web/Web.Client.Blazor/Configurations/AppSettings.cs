namespace Web.Client.Blazor.Configurations;

public class AppSettings
{
    public string? ApiBaseUrl { get; set; }
    public int TimeoutSeconds { get; set; }
    public string? CacheFilePath { get; set; }
}
