using Web.Client.Blazor.Dtos;

namespace Web.Client.Blazor.Utilities.Api;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;

    public ApiClient(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("DHT");
    }

    public async Task<AccountInfo> FetchAccountData(AccountRequest request, string apiEndPoint)
    {
        try
        {
            var apiResponse = await _httpClient.PostAsJsonAsync(apiEndPoint, request);
            if (!apiResponse.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Error calling API: {apiResponse.StatusCode}");
            }
            return await apiResponse.Content.ReadFromJsonAsync<AccountInfo>()
                ?? throw new InvalidOperationException("Invalid response from API.");
        }
        catch (Exception)
        {

            throw;
        }
    }
}
