using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using Web.Client.Blazor.Components;
using Web.Client.Blazor.Configurations;
using Web.Client.Blazor.Utilities.Api;
using Web.Client.Blazor.Utilities.Caching;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

builder.Services.AddHttpClient("DHT", client =>
{
    client.BaseAddress = new Uri(appSettings!.ApiBaseUrl!);
    client.Timeout = TimeSpan.FromSeconds(appSettings.TimeoutSeconds);
});

var cacheSettings = new CacheSetting();
builder.Configuration.GetSection("CacheSettings").Bind(cacheSettings);
builder.Services.AddSingleton(cacheSettings);

builder.Services.AddMemoryCache();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = cacheSettings.Redis!.Configuration;
    options.InstanceName = cacheSettings.Redis.InstanceName;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(cacheSettings.Redis!.Configuration!));
builder.Services.AddScoped(sp =>
{
    var connectionMultiplexer = sp.GetRequiredService<IConnectionMultiplexer>();
    return connectionMultiplexer.GetDatabase();
});

builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
//builder.Services.AddScoped<IMemCacheService, InMemoryCacheService>();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
