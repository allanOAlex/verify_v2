using Quartz;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;
using Verify.Application.Abstractions.IServices;

namespace Verify.Application.Abstractions.Interfaces;
public interface IServiceManager
{
    ICacheService CacheService { get; }
    ILogService LogService { get; }
    IDhtService DhtService { get; }
    IDhtRedisService DhtRedisService { get; }
    IHashingService HashingService { get; }
    INodeManagementService NodeManagementService { get; }
    IDhtMaintenanceJob DhtMaintenanceJob { get; }
    IAddNodeToPeersJob AddNodeToPeersJob { get; }


}
