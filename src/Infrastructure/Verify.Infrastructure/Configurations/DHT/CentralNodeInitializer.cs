using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Bank;

namespace Verify.Infrastructure.Configurations.DHT;


public sealed class CentralNodeInitializer : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;

    public CentralNodeInitializer(IServiceScopeFactory scopeFactory, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        //using (var scope = _scopeFactory.CreateScope())

        var scope = _scopeFactory.CreateScope();
        var _dhtRedisService = scope.ServiceProvider.GetRequiredService<IDhtRedisService>();
        var _hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        // Fetch the central node's BIC from configuration
        var centralNodeBic = _configuration["NodeConfig:CurrentNode"];
        if (string.IsNullOrEmpty(centralNodeBic))
        {
            throw new InvalidOperationException("Central node BIC not configured.");
        }

        // Hash the BIC and check if the central node exists
        var centralNodeHash = await _hashingService.ByteHash(centralNodeBic);
        var nodeExistsResponse = await _dhtRedisService.NodeExistsAsync("dht:nodes", centralNodeHash.Data!);
        if (!nodeExistsResponse.Data)
        {
            var nodeEndpoint = _configuration["NodeConfig:DHTNODE"];
            NodeInfo centralNode = new()
            {
                NodeBic = centralNodeBic,
                NodeHash = centralNodeHash.Data!,
                NodeEndPoint = nodeEndpoint,
                NodeUri = new Uri(nodeEndpoint!),
                LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var serializedCentralNode = JsonSerializer.Serialize(centralNode);
            // Add the central node to Redis without expiry
            await _dhtRedisService.SetSortedNodeAsync($"dht:buckets:{{0}}", serializedCentralNode, 0);
            await _dhtRedisService.SetNodeAsync("dht:nodes", centralNodeHash.Data!, serializedCentralNode, isCentralNode: true);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;


}
