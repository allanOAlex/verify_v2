using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;


internal sealed class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;


    public ApiClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient("DHT");
    }

    public async Task<AccountInfo> FetchAccountData(AccountRequest request, string apiEndPoint)
    {
        var apiResponse = await _httpClient.PostAsJsonAsync(apiEndPoint, request);
        if (!apiResponse.IsSuccessStatusCode)
        {

        }
        apiResponse.EnsureSuccessStatusCode();
        var accountInfoResponse = await apiResponse.Content.ReadFromJsonAsync<AccountInfo>();
        return accountInfoResponse!;
    }

}
