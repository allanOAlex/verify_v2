// See https://aka.ms/new-console-template for more information
using System.Diagnostics;

using BenchmarkDotNet.Running;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Client.Console.ApiClients;
using Web.Client.Console.Configurations;
using Web.Client.Console.Dtos;
using Web.Client.Console.Utilities;


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
serviceCollection.AddSingleton<IApiClient, ApiClient>();

var serviceProvider = serviceCollection.BuildServiceProvider();

var config = serviceProvider.GetRequiredService<IConfiguration>();
var apiClient = serviceProvider.GetRequiredService<IApiClient>();

#endregion

#region FetchAccountInfo

AccountRequest accountRequest = new()
{
    InitiatorBIC = "BARCKENX",
    RecipientBIC = "SCBLKENX",
    RecipientAccountNumber = "2456345646"
};

AccountRequest accountRequest1 = new()
{
    InitiatorBIC = "SCBLKENX",
    RecipientBIC = "BARCKENX",
    RecipientAccountNumber = "2456345647"
};

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
		try
		{
            var accountResponse = await apiClient.FetchAccountData(request, apiEndPoint);
            return accountResponse;
        }
		catch (Exception)
		{

			throw;
		}
    }
}