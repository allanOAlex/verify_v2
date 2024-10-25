using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Web.Client.Console.ApiClients;
using Web.Client.Console.Configurations;
using Web.Client.Console.Dtos;

namespace Web.Client.Console.Utilities;
internal class ApiBenchmark
{
    private readonly IApiClient _apiClient;
    private readonly IConfiguration _config;
    private readonly AccountRequest _accountRequest;

    public ApiBenchmark()
    {
        var serviceCollection = new ServiceCollection();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var appSettings = configuration.GetSection("AppSettings").Get<AppSettings>();

        serviceCollection.AddHttpClient("DHT", client =>
        {
            client.BaseAddress = new Uri(appSettings!.ApiBaseUrl!);
            client.Timeout = TimeSpan.FromSeconds(appSettings.TimeoutSeconds);
        });

        serviceCollection.AddSingleton<IConfiguration>(configuration);
        serviceCollection.AddSingleton<IApiClient, ApiClient>();

        var serviceProvider = serviceCollection.BuildServiceProvider();

        _config = serviceProvider.GetRequiredService<IConfiguration>();
        _apiClient = serviceProvider.GetRequiredService<IApiClient>();

        // Initialize AccountRequest
        _accountRequest = new AccountRequest
        {
            InitiatorBIC = "SCBLKENX",
            RecipientBIC = "BARCKENX",
            RecipientAccountNumber = "2456345645"
        };
    }

    [Benchmark]
    public async Task<AccountInfo> BenchmarkFetchAccountData()
    {
        var _apiEndPoint = _config["AppSettings:EndPoints:DHT:FetchAccountInfo"];
        return await Methods.FetchAccountData(_apiClient, _accountRequest, _apiEndPoint!);
    }
}
