using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IDhtService
{
    
    //Task<DhtResponse<bool>> AddNodeToPeers(NodeInfo nodeInfo);
    Task<DhtResponse<bool>> AddNodeToPeers(List<NodeInfo> nodes, byte[] centralNodeHash, byte[] senderBicHash, byte[] recipinetBicHash);
    Task<DhtResponse<AccountInfo>> StoreAccountDataAsync(AccountInfo accountInfo);
    Task<DhtResponse<AccountInfo>> LookupAccountInMemoryAsync(AccountRequest accountRequest);
    Task<DhtResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash);
    Task<DhtResponse<AccountInfo>> FetchAccountData(AccountRequest accountRequest);
    Task<DhtResponse<AccountInfo>> QueryBankAsync(string queryUrl, AccountRequest accountRequest);

}
