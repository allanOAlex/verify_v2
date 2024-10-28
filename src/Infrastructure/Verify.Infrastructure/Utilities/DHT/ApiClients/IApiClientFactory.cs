namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal interface IApiClientFactory
{
    IApiClient CreateClient(string nodeBaseUrl);
}
