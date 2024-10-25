using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Abstractions.IServices;

namespace Verify.Infrastructure.Implementations.Interfaces;
internal sealed class ServiceManager : IServiceManager
{
    public ICacheService CacheService { get; }
    public ILogService LogService { get; }
    public IDHTService DHTService { get; }
    public IDHTRedisService DHTRedisService { get; }
    public IHashingService HashingService { get; }
    public INodeManagementService NodeManagementService { get; }
    public IDHTMaintenanceJob DHTMaintenanceJob { get; }




    public ServiceManager(
        ICacheService cacheService, 
        ILogService logService, 
        IDHTService dHTService,
        IDHTRedisService dHTRedisService,
        IHashingService hashingService,
        INodeManagementService nodeManagementService,
        IDHTMaintenanceJob dHTMaintenanceJob)
    {
        CacheService = cacheService;
        LogService = logService;
        DHTService = dHTService;
        DHTRedisService = dHTRedisService;
        HashingService = hashingService;
        NodeManagementService = nodeManagementService;
        DHTMaintenanceJob = dHTMaintenanceJob;
    }
}
