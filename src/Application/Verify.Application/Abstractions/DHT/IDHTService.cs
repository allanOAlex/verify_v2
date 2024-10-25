using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IDHTService
{
    
    Task<DHTResponse<bool>> AddNodeToPeers(NodeInfo nodeInfo);
    Task<DHTResponse<AccountInfo>> StoreAccountDataAsync(AccountInfo accountInfo);
    Task<DHTResponse<AccountInfo>> LookupAccountInMemoryAsync(AccountRequest accountRequest);
    Task<DHTResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash);
    Task<DHTResponse<NodeInfo>> GetClosestNode(byte[] accountHash);
    Task<DHTResponse<bool>> NodeHasDataForKeyAsync(NodeInfo nodeInfo, byte[] accountHash);
    Task<DHTResponse<AccountInfo>> FetchAccountData(AccountRequest accountRequest);
    Task<DHTResponse<AccountInfo>> QueryBankAsync(string queryUrl, AccountRequest accountRequest);

}
