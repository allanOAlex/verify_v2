using Web.Client.Console.Dtos;

namespace Web.Client.Console.ApiClients;
public interface IApiClient
{
    Task<AccountInfo> FetchAccountData(AccountRequest request, string apiEndPoint);
}
