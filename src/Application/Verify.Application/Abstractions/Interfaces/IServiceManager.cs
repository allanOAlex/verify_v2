using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;
using Verify.Application.Abstractions.IServices;

namespace Verify.Application.Abstractions.Interfaces;
public interface IServiceManager
{
    ICacheService CacheService { get; }
    ILogService LogService { get; }
    IDHTService DHTService { get; }
    IDHTRedisService DHTRedisService { get; }
    IHashingService HashingService { get; }
    INodeManagementService NodeManagementService { get; }
    IDHTMaintenanceJob DHTMaintenanceJob { get; }


}
