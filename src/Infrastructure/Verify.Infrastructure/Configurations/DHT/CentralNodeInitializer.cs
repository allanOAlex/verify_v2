using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Bank;
using Verify.Infrastructure.Implementations.DHT;

namespace Verify.Infrastructure.Configurations.DHT;
internal sealed class CentralNodeInitializer : IHostedService
{
    private readonly IDHTRedisService dhtRedisService;
    private readonly IHashingService hashingService;
    private readonly IConfiguration configuration;

    public CentralNodeInitializer(IDHTRedisService DhtRedisService, IHashingService HashingService, IConfiguration Configuration)
    {
        dhtRedisService = DhtRedisService;
        hashingService = HashingService;
        configuration = Configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Fetch the central node's BIC from configuration
        var centralNodeBIC = configuration["NodeConfig:CurrentNode"];
        if (string.IsNullOrEmpty(centralNodeBIC))
        {
            throw new InvalidOperationException("Central node BIC not configured.");
        }

        // Hash the BIC and check if the central node exists
        var centralNodeHash = await hashingService.ByteHash(centralNodeBIC);
        var nodeExistsResponse = await dhtRedisService.NodeExistsAsync("dht:nodes", centralNodeHash.Data!);
        if (!nodeExistsResponse.Data)
        {
            var nodeEndpoint = configuration["NodeConfig:DHTNODE"];
            NodeInfo centralNode = new()
            {
                NodeBIC = centralNodeBIC,
                NodeHash = centralNodeHash.Data!,
                NodeEndPoint = nodeEndpoint,
                NodeUri = new Uri(nodeEndpoint!),
                LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            // Add the central node to Redis without expiry
            await dhtRedisService.SetSortedNodeAsync($"dht:buckets:{0}", centralNode, 0);
            await dhtRedisService.SetNodeAsync("dht:nodes", centralNodeHash.Data!, JsonConvert.SerializeObject(centralNode), null, isCentralNode: true);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


}
