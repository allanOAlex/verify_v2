using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IServices;

namespace Verify.Infrastructure.Implementations.Interfaces;
internal sealed class ServiceManager : IServiceManager
{
    public ICacheService CacheService { get; }
    public ILogService LogService { get; }
    public IDhtService DhtService { get; }
    public IDhtRedisService DhtRedisService { get; }
    public IHashingService HashingService { get; }
    public INodeManagementService NodeManagementService { get; }
    public IDhtMaintenanceJob DhtMaintenanceJob { get; }
    public IAddNodeToPeersJob AddNodeToPeersJob { get; }




    public ServiceManager(
        ICacheService cacheService, 
        ILogService logService, 
        IDhtService dHtService,
        IDhtRedisService dHtRedisService,
        IHashingService hashingService,
        INodeManagementService nodeManagementService,
        IDhtMaintenanceJob dHtMaintenanceJob,
        IAddNodeToPeersJob addNodeToPeersJob
        )
    {
        CacheService = cacheService;
        LogService = logService;
        DhtService = dHtService;
        DhtRedisService = dHtRedisService;
        HashingService = hashingService;
        NodeManagementService = nodeManagementService;
        DhtMaintenanceJob = dHtMaintenanceJob;
        AddNodeToPeersJob = addNodeToPeersJob;
    }
}
