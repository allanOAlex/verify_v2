using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Refit;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Utilities.DHT.ApiClients;
internal interface IApiClient
{
    [Post("/api/account/fetchaccountinfo")]
    Task<AccountResponse> FetchAccountData([Body] AccountRequest fetchAccountRequest);




}
