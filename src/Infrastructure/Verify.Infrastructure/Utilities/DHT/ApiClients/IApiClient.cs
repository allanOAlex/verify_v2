using Refit;
using Verify.Application.Dtos.Account;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;


internal interface IApiClient
{
    [Get("/api/account/ping")]
    Task<AccountResponse> PingNodeAsync();

    [Post("/api/account/fetchaccountinfo")]
    Task<AccountResponse> FetchAccountData([Body] AccountRequest fetchAccountRequest);

}
