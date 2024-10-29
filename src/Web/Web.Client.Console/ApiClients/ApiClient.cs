using System.Net.Http.Json;
using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;


internal sealed class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;


    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("DHT");
        _httpClient.BaseAddress = new Uri("https://localhost:7260/");
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
