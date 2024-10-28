using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface INodeManagementService
{

    Task<DhtResponse<string>> GetNodeEndpointAsync(byte[] accountHash);
    Task<DhtResponse<string>> GetNodeEndpointFromConfigAsync(string bankBic);
    Task<DhtResponse<bool>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, bool withEviction = true);
    Task<DhtResponse<bool>> PingNodeAsync(NodeInfo node);



}
