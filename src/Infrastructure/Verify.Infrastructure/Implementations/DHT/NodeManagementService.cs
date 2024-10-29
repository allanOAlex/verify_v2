using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Polly;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Infrastructure.Utilities.DHT.ApiClients;

namespace Verify.Infrastructure.Implementations.DHT;


internal sealed class NodeManagementService : INodeManagementService
{
    private readonly HttpClient _httpClient;
    private readonly IHashingService _hashingService;
    private readonly IDhtRedisService _dhtRedisService;
    private readonly IConfiguration _configuration;
    private readonly IApiClientFactory _apiClientFactory;
    private const int BucketSize = 1;


    public NodeManagementService(
        IHttpClientFactory httpClientFactory,
        IHashingService hashingService,
        IDhtRedisService dhtRedisService,
        IConfiguration configuration,
        IApiClientFactory apiClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("Node");

        _hashingService = hashingService;
        _dhtRedisService = dhtRedisService;
        _configuration = configuration;
        _apiClientFactory = apiClientFactory;
    }


    public async Task<DhtResponse<string>> GetNodeEndpointAsync(byte[] accountHash)
    {
        var dhtResponse = await _dhtRedisService.GetNodeAsync("dht:nodes", accountHash);
        if (dhtResponse.Successful && dhtResponse.Data != null)
        {
            if (!string.IsNullOrEmpty( dhtResponse.Data.NodeEndPoint))
            {
                return DhtResponse<string>.Success("Node endpoint retrieved successfully",  dhtResponse.Data.NodeEndPoint);
            }
        }

        return DhtResponse<string>.Failure("Node endpoint not found.");
    }

    public DhtResponse<string> GetNodeEndpointFromConfigAsync(string bankBic)
    {
        var nodeEnpoint = _configuration[$"NodeConfig:{bankBic}"];
        if (!string.IsNullOrWhiteSpace(nodeEnpoint))
        {
            return DhtResponse<string>.Success("Success", nodeEnpoint);
        }

        return DhtResponse<string>.Failure("No Endpoint defined for this particular node", string.Empty);
    }

    public async Task<DhtResponse<bool>> AddOrUpdateNodeAsync(NodeInfo nodeInfo, bool withEviction = true)
    {
        var isCentralNode = nodeInfo.NodeBic == _configuration["NodeConfig:CurrentNode"];

        // ToDo: BUG! BUG! BUG! - NodeInfo (Current Node) is the node we are trying to add; we eed to have a current node to compare the distance
        var currentNodeHash = await _hashingService.ByteHash(_configuration["NodeConfig:CurrentNode"]!);
        int distance = DhtUtilities.CalculateXorDistance(currentNodeHash.Data!, nodeInfo.NodeHash);

        string redisBucketsKey = $"dht:buckets:{distance}";
        string redisNodesKey = $"dht:nodes";

        var existingNodeResponse = await _dhtRedisService.GetNodeAsync(redisNodesKey, nodeInfo.NodeHash);
        if (existingNodeResponse.Data != null)
        {
            var updatedNode = UpdateNodeInfo(existingNodeResponse.Data!, nodeInfo);
            var serializedNode = JsonSerializer.Serialize(updatedNode);
            await _dhtRedisService.SetNodeAsync(redisNodesKey, currentNodeHash.Data!, serializedNode, TimeSpan.FromHours(24), isCentralNode);
            await _dhtRedisService.SetSortedNodeAsync(redisBucketsKey, serializedNode, distance);

            return DhtResponse<bool>.Success("Node updated successfully", true, null, new Dictionary<string, object>() { { "node", updatedNode } });
        }
        else
        {
            return await HandleNodeAdditionWithEvictionAsync(redisBucketsKey, redisNodesKey, nodeInfo, distance, isCentralNode, withEviction);
        }
    }

    public async Task<DhtResponse<bool>> PingNodeAsync(NodeInfo nodeInfo)
    {
        var pingUri = new Uri(nodeInfo.NodeUri, "ping");
        var pingUrl = $"{nodeInfo.NodeUri.Scheme}://{nodeInfo.NodeUri.Host}:{nodeInfo.NodeUri.Port}/";

        // Define the retry policy with exponential backoff
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>() // Retry for both network errors and timeouts
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)) // Exponential backoff: 2s, 4s, 8s
            );

        var result = await retryPolicy.ExecuteAsync(async () =>
        {
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                var nodeApiClient = _apiClientFactory.CreateClient(pingUrl);
                var pingNodeResponse = await nodeApiClient.PingNodeAsync();

                var response = await _httpClient.GetAsync(pingUri, cts.Token);
                return response.IsSuccessStatusCode;
            }
        });

        return DhtResponse<bool>.Success(
            "",
            result);
    }



    #region Private Methods

    private NodeInfo UpdateNodeInfo(NodeInfo existingNode, NodeInfo newNode)
    {
        return new NodeInfo
        {
            NodeBic = newNode.NodeBic,
            NodeUri = newNode.NodeUri,
            NodeHash = newNode.NodeHash,
            NodeEndPoint = newNode.NodeEndPoint,
            LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            KnownPeers = newNode.KnownPeers ?? existingNode.KnownPeers
        };
    }

    private async Task<DhtResponse<bool>> HandleNodeAdditionWithEvictionAsync(string redisBucketsKey, string redisNodesKey, NodeInfo nodeInfo, int distance, bool isCentralNode, bool withEviction)
    {
        // Check bucket size for the XOR distance bucket
        var bucketCount = await _dhtRedisService.GetBucketCountAsync(redisBucketsKey);
        if (bucketCount.Data < BucketSize)
        {
            var serializedNode = JsonSerializer.Serialize(nodeInfo);
            await _dhtRedisService.SetNodeAsync(redisNodesKey, nodeInfo.NodeHash, serializedNode, TimeSpan.FromHours(24), isCentralNode);
            return await _dhtRedisService.SetSortedNodeAsync(redisBucketsKey, serializedNode, distance);

        }
        else if (withEviction)
        {
            return await EvictAndReplaceNode(redisBucketsKey, redisNodesKey, nodeInfo, distance, isCentralNode);
        }
        else
        {
            return DhtResponse<bool>.Failure("Bucket is full, and eviction is disabled.");
        }
    }

    private async Task<DhtResponse<bool>> EvictAndReplaceNode(string redisBucketsKey, string redisNodesKey, NodeInfo newNode, int distance, bool isCentralNode)
    {
        // ToDo: What if there is no nodes in the bucket?
        if (await _dhtRedisService.GetLeastRecentlySeenNodeAsync(redisBucketsKey, redisBucketsKey) is { } leastRecentlySeenNode)
        {
            var isReachable = await PingNodeAsync(leastRecentlySeenNode.Data!);
            if (!isReachable.Data)
            {
                var serializedNode = JsonSerializer.Serialize(newNode);
                await _dhtRedisService.RemoveSortedSetNodeAsync(redisNodesKey, leastRecentlySeenNode.Data!);
                await _dhtRedisService.SetSortedNodeAsync(redisBucketsKey, serializedNode, distance);
                await _dhtRedisService.SetNodeAsync(redisBucketsKey, newNode.NodeHash, serializedNode, TimeSpan.FromHours(24), isCentralNode);
                return DhtResponse<bool>.Success("Replaced least recently seen node", true, null, new Dictionary<string, object>() { { "node", newNode } });
            }

            // If reachable, check if we should replace the least recently seen node based on LRS/LRU
            if (ShouldReplaceNode(leastRecentlySeenNode.Data!, newNode, isCentralNode))
            {
                var serializedNodeToAdd = JsonSerializer.Serialize(newNode);
                await _dhtRedisService.RemoveSortedSetNodeAsync(redisNodesKey, leastRecentlySeenNode.Data!);
                await _dhtRedisService.SetSortedNodeAsync(redisBucketsKey, serializedNodeToAdd, distance);
                return DhtResponse<bool>.Success("Replaced least recently seen node based on LRS policy", true, null, new Dictionary<string, object>() { { "node", newNode } });
            }
            else
            {
                return DhtResponse<bool>.Failure("Bucket is full, and all nodes are reachable.");
            }
        }

        var serializedNewNode = JsonSerializer.Serialize(newNode);
        await _dhtRedisService.SetSortedNodeAsync(redisBucketsKey, serializedNewNode, distance);
        await _dhtRedisService.SetNodeAsync(redisBucketsKey, newNode.NodeHash, serializedNewNode, TimeSpan.FromHours(24), isCentralNode);
        return DhtResponse<bool>.Success("No node to replace: - added new node successfully", true, null, new Dictionary<string, object>() { { "node", newNode } });

    }

    private bool ShouldReplaceNode(NodeInfo leastRecentlySeenNode, NodeInfo newNode, bool isCentralNode)
    {
        // Customize your eviction policy here, e.g., based on age, frequency, or node metrics
        // For example, we'll replace if the new node has been seen more recently than the least recently seen node
        if (isCentralNode)
        {
            return false;
        }
        return leastRecentlySeenNode.LastSeen < newNode.LastSeen;
    }

    private void QueueRejectedNodeForFuture(NodeInfo rejectedNode)
    {
        // Implement a queue to reattempt adding the rejected node later
        // This could be a Redis queue, a memory cache, or a database table for retries
        // For demonstration, we will simulate a queue using a ConcurrentQueue
        var rejectedNodesQueue = new ConcurrentQueue<NodeInfo>();
        rejectedNodesQueue.Enqueue(rejectedNode);
    }

   



    #endregion



}
