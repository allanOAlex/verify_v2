using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal interface IApiClientFactory
{
    IApiClient CreateClient(string nodeBaseUrl);
}
