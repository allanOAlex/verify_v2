using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Refit;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal sealed class ApiClientFactory : IApiClientFactory
{
    private readonly RefitSettings refitSettings;

    public ApiClientFactory(RefitSettings RefitSettings)
    {
        refitSettings = RefitSettings;

    }

    public IApiClient CreateClient(string nodeBaseUrl)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nodeBaseUrl))
            {
                throw new ArgumentException("Bank base URL cannot be null or empty", nameof(nodeBaseUrl));
            }

            // Create and configure an HttpClient with timeout, etc.
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri(nodeBaseUrl),
                Timeout = TimeSpan.FromSeconds(500)  // You can adjust the timeout as necessary
            };

            // Create and return a Refit client dynamically
            return RestService.For<IApiClient>(httpClient, refitSettings);
        }
        catch (Exception)
        {

            throw;
        }
    }
}
