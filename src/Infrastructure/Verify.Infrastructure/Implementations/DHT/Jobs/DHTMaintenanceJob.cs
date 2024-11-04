using Quartz;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;

namespace Verify.Infrastructure.Implementations.DHT.Jobs
{
    internal sealed class DhtMaintenanceJob : IDhtMaintenanceJob
    {
        private readonly IDhtRedisService _dHtRedisService;

        public DhtMaintenanceJob(IDhtRedisService dhtRedisService)
        {
            _dHtRedisService = dhtRedisService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _dHtRedisService.CleanUpInactiveNodesAsync("dht:nodes");
            
        }
    }
}
