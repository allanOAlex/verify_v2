// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Client.Console.ApiClients;
using Web.Client.Console.Configurations;
using Web.Client.Console.Dtos;
using Web.Client.Console.Utilities.Caching;


#region Services and DI

var serviceCollection = new ServiceCollection();

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)  // Set the base path to the current directory
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Bind configuration settings to a POCO
var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

serviceCollection.AddHttpClient("DHT", client =>
{
    client.BaseAddress = new Uri(appSettings!.ApiBaseUrl!);
    client.Timeout = TimeSpan.FromSeconds(appSettings.TimeoutSeconds);

});

serviceCollection.AddSingleton<IConfiguration>(configuration);
serviceCollection.AddScoped<IApiClient, ApiClient>();

serviceCollection.AddMemoryCache();
//serviceCollection.AddSingleton<ICacheService, InMemoryCacheService>();
serviceCollection.AddSingleton<ICacheService, FileCacheService>();
serviceCollection.AddSingleton<ICacheService>(provider =>
{
    // Check if directory exists
    if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Verify", "Cache")))
    {
        Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Verify", "Cache"));
    }
    return new FileCacheService(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Verify", "Cache"));
});


var serviceProvider = serviceCollection.BuildServiceProvider();

var config = serviceProvider.GetRequiredService<IConfiguration>();
var apiClient = serviceProvider.GetRequiredService<IApiClient>();
var fileCacheService = serviceProvider.GetRequiredService<ICacheService>();

#endregion

#region FetchAccountInfo


AccountRequest accountRequest1 = new()
{
    SenderBic = "BARCKENX",
    RecipientBic = "SCBLKENX",
    RecipientAccountNumber = "2456345646"
};

AccountRequest accountRequest2 = new()
{
    SenderBic = "SCBLKENX",
    RecipientBic = "BARCKENX",
    RecipientAccountNumber = "2456345647"
};

//Stopwatch responseTime = Stopwatch.StartNew();

//var cacheKey = CacheKeyHelper.GenerateCacheKey(accountRequest1.RecipientBic, accountRequest1.RecipientAccountNumber);
//var cachedAccountInfo = fileCacheService.Get<AccountInfo>(cacheKey);

//responseTime.Stop();

//if (cachedAccountInfo != null)
//{
//    Console.WriteLine($"Account Holder: {cachedAccountInfo.AccountName}");
//    Console.WriteLine();
//    Console.WriteLine("Account Number: " + cachedAccountInfo.AccountNumber);
//    Console.WriteLine();
//    Console.WriteLine($"Time taken: {responseTime.ElapsedMilliseconds} ms");
//    Console.ReadKey();
//}
//else
//{
//    Stopwatch stopwatch = Stopwatch.StartNew();

//    var apiEndPoint = config["AppSettings:EndPoints:DHT:FetchAccountInfo"];
//    var verifyResponse = await Methods.FetchAccountData(apiClient, accountRequest1, apiEndPoint!);

//    stopwatch.Stop();

//    Console.WriteLine($"Account Holder: {verifyResponse.AccountName}");
//    Console.WriteLine();
//    Console.WriteLine("Account Number: " + verifyResponse.AccountNumber);

//    fileCacheService.Set(cacheKey, verifyResponse, TimeSpan.FromHours(12), TimeSpan.FromHours(12));

//    Console.WriteLine();
//    Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
//    Console.ReadKey();

//}

Stopwatch stopwatch = Stopwatch.StartNew();

var apiEndPoint = config["AppSettings:EndPoints:DHT:FetchAccountInfo"];
var verifyResponse = await Methods.FetchAccountData(apiClient, accountRequest1, apiEndPoint!);

stopwatch.Stop();

Console.WriteLine($"Account Holder: {verifyResponse.AccountName}");
Console.WriteLine();
Console.WriteLine("Account Number: " + verifyResponse.AccountNumber);

Console.WriteLine();
Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
Console.ReadKey();


#endregion

public class Methods
{
    public async static Task<AccountInfo> FetchAccountData(IApiClient apiClient, AccountRequest request, string apiEndPoint)
    {
        var accountResponse = await apiClient.FetchAccountData(request, apiEndPoint);
        return accountResponse;
    }
}