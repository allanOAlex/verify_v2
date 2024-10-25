using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;

namespace Verify.Infrastructure.Implementations.DHT.Jobs
{
    internal sealed class DHTMaintenanceJob : IDHTMaintenanceJob
    {
        private readonly IDHTRedisService dHTRedisService;

        public DHTMaintenanceJob(IDHTRedisService DHTRedisService)
        {
            dHTRedisService = DHTRedisService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await dHTRedisService.CleanUpInactiveNodesAsync("dht:nodes");

                // We could add other maintenance tasks here
                

                // Log or report the task completion
                
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
