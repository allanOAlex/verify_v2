using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;
public interface IApiClient
{
    Task<AccountInfo> FetchAccountData(AccountRequest request, string apiEndPoint);
}
