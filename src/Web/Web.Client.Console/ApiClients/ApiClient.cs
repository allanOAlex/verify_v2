using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;
internal sealed class ApiClient : IApiClient
{
    private readonly HttpClient httpClient;
    private readonly IConfiguration configuration;
    public ApiClient(IHttpClientFactory httpClientFactory, IConfiguration Configuration)
    {
        configuration = Configuration;
        httpClient = httpClientFactory.CreateClient("DHT");
    }

    public async Task<AccountInfo> FetchAccountData(AccountRequest request, string apiEndPoint)
    {
        try
        {
            var apiResponse = await httpClient.PostAsJsonAsync(apiEndPoint, request);
            if (!apiResponse.IsSuccessStatusCode)
            {

            }
            apiResponse.EnsureSuccessStatusCode();
            var accountInfoResponse = await apiResponse.Content.ReadFromJsonAsync<AccountInfo>();
            return accountInfoResponse!;
        }
        catch (Exception)
        {

            throw;
        }
    }

}
