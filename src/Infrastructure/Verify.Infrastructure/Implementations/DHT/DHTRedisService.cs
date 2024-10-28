﻿using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Shared.Utilities;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DhtRedisService : IDhtRedisService
{
    private readonly IDatabase _redisDatabase;
    private readonly IConfiguration _configuration;
    private readonly IHashingService _hashingService;

    public DhtRedisService(IDatabase redisDatabase, IConfiguration configuration, IHashingService hashingService)
    {
        _redisDatabase = redisDatabase;
        _configuration = configuration;
        _hashingService = hashingService;
            
    }


    public async Task<ITransaction> CreateTransaction()
    {
        return _redisDatabase.CreateTransaction();
    }

    public async Task<DhtResponse<bool>> NodeExistsAsync(string key, byte[] hash)
    {
        try
        {
            bool exists = await _redisDatabase.HashExistsAsync(key, hash);
            string message = exists
                ? $"Node {Convert.ToBase64String(hash)} exists"
                : $"Node {Convert.ToBase64String(hash)} does not exist";

            return exists
                ? DhtResponse<bool>.Success(message, true)
                : DhtResponse<bool>.Failure(message, false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<bool>> SortedSetNodeExistsByScoreAsync(string key, string serializedValue)
    {
        try
        {
            var exists = await _redisDatabase.SortedSetScoreAsync(key, serializedValue);
            return exists.HasValue
                ? DhtResponse<bool>.Success("Node exists", true)
                : DhtResponse<bool>.Failure("Node does not exist", false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<bool>> SortedSetNodeExistsByRankAsync(string key, byte[] hash)
    {
        try
        {
            var exists = await _redisDatabase.SortedSetRankAsync(key, JsonConvert.SerializeObject(hash));
            return exists.HasValue
                ? DhtResponse<bool>.Success("Node exists", true)
                : DhtResponse<bool>.Failure("Node does not exist", false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<NodeInfo>> GetNodeAsync(string key, byte[] field)
    {
        try
        {
            var nodeData = await _redisDatabase.HashGetAsync(key, field);
            if (nodeData.IsNullOrEmpty)
            {
                return DhtResponse<NodeInfo>.Failure("Node not found.");
            }

            var node = JsonConvert.DeserializeObject<NodeInfo>(nodeData!);
            return DhtResponse<NodeInfo>.Success("Node retrieved successfully", node!);
        }
        catch (JsonException jsonEx)
        {
            throw new Exception("Error deserializing the node information from Redis.", jsonEx);
        }
    }

    public async Task<DhtResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodeList = new();
            var nodes = await _redisDatabase.SortedSetRangeByScoreAsync(key, minScore, maxScore);
            foreach (var node in nodes)
            {
                var serializedNodeInfo = await _redisDatabase.HashGetAsync(key, node);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodeList.Add(DeserializeNodeInfo(serializedNodeInfo));
                }
            }

            return nodes.Any()
                 ? DhtResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodeList)
                 : DhtResponse<List<NodeInfo>>.Failure("No nodes found in the range", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving nodes by score range: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<List<NodeInfo>>> GetNodesByRankRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodeList = new();
            var nodes = await _redisDatabase.SortedSetRangeByRankAsync(key, minScore, maxScore);
            foreach (var node in nodes)
            {
                var serializedNodeInfo = await _redisDatabase.HashGetAsync(key, node);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodeList.Add(DeserializeNodeInfo(serializedNodeInfo));
                }
            }

            return nodes.Any()
                 ? DhtResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodeList)
                 : DhtResponse<List<NodeInfo>>.Failure("No nodes found in the range", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving nodes by score range: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<NodeInfo>> GetSortedSetClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash)
    {
        try
        {
            int distance = DhtUtilities.CalculateXorDistance(currentNodeHash, bicHash); 
            string redisBucketsKey = $"dht:buckets:{distance}";

            // Retrieve nodes by ascending order of XOR distance
            var sortedNodes = await _redisDatabase.SortedSetRangeByScoreAsync(redisBucketsKey, 0, double.MaxValue, Exclude.None, Order.Ascending);

            // Filter to include only those nodes with the same bicHash
            var filteredNodes = sortedNodes
                .Select(nodeData => JsonConvert.DeserializeObject<NodeInfo>(nodeData!))
                .Where(node => node!.NodeHash.SequenceEqual(bicHash))
                .ToList();

            if (filteredNodes == null || !filteredNodes.Any())
            {
                return DhtResponse<NodeInfo>.Failure("No nodes found for the given BIC hash.");
            }

            NodeInfo closestNode = null!;
            long closestDistance = long.MaxValue;

            Parallel.ForEach(filteredNodes, node =>
            {
                var currentDistance = DhtUtilities.CalculateXorDistance(bicHash, node!.NodeHash);
                if (currentDistance < Interlocked.CompareExchange(ref closestDistance, currentDistance, closestDistance)) // Interlocked to safely update the closest node
                {
                    NodeInfo currentClosestNode = node; // Store the closest node in a thread-safe manner
                    Interlocked.Exchange(ref closestNode, currentClosestNode); // This might cause issues because `closestNode` is not thread-safe.( We can use Interlocked to manage this safely)
                }
            });

            return closestNode != null
                ? DhtResponse<NodeInfo>.Success("Closest node found successfully.", closestNode)
                : DhtResponse<NodeInfo>.Failure("No closest node found in the DHT.", null);
        }
        catch (Exception ex)
        {
            return DhtResponse<NodeInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DhtResponse<NodeInfo>> GetClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash)
    {
        try
        {
            int distance = DhtUtilities.CalculateXorDistance(currentNodeHash, bicHash);
            string redisBucketsKey = $"dht:buckets:{distance}";

            // Retrieve all nodes in the bucket
            var sortedNodes = await _redisDatabase.SortedSetRangeByScoreAsync(redisBucketsKey, 0, double.MaxValue, Exclude.None, Order.Ascending);
            if (!sortedNodes.Any())
            {
                return DhtResponse<NodeInfo>.Failure("No nodes found for the given BIC hash.");
            }

            // Filter nodes with matching bicHash concurrently
            var tasks = sortedNodes.Select(async nodeData =>
            {
                var node = JsonConvert.DeserializeObject<NodeInfo>(nodeData!);
                return node!.NodeHash.SequenceEqual(bicHash) ? node : null;
            });

            // Wait for all filtering tasks to complete
            var filteredNodes = await Task.WhenAll(tasks);

            // Find the closest node (avoiding Parallel.ForEach)
            NodeInfo closestNode = null!;
            long closestDistance = long.MaxValue;
            foreach (var node in filteredNodes)
            {
                if (node != null)
                {
                    var currentDistance = DhtUtilities.CalculateXorDistance(bicHash, node.NodeHash);
                    if (currentDistance < closestDistance)
                    {
                        closestDistance = currentDistance;
                        closestNode = node;
                    }
                }
            }

            return closestNode != null
                ? DhtResponse<NodeInfo>.Success("Closest node found successfully.", closestNode)
                : DhtResponse<NodeInfo>.Failure("No closest node found in the DHT.", null);
        }
        catch (Exception ex)
        {
            return DhtResponse<NodeInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DhtResponse<NodeInfo>> FindClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash, int maxDepth = 3)
    {
        try
        {
            int distance = DhtUtilities.CalculateXorDistance(currentNodeHash, bicHash);
            string redisBucketsKey = $"dht:buckets:{distance}";

            // Retrieve the first node
            var firstNodeData = await _redisDatabase.SortedSetRangeByScoreAsync(redisBucketsKey, 0, double.MaxValue, Exclude.None, Order.Ascending, 0, 1);
            if (!firstNodeData.Any())
            {
                return DhtResponse<NodeInfo>.Failure("No nodes found for the given BIC hash.");
            }

            var firstNode = JsonConvert.DeserializeObject<NodeInfo>(firstNodeData.First()!);

            // Check for exact match first
            if (firstNode!.NodeHash.SequenceEqual(bicHash))
            {
                return DhtResponse<NodeInfo>.Success("Exact BIC match found.", firstNode);
            }

            // If not exact match and max depth not reached, recurse
            if (maxDepth > 0)
            {
                // Calculate the distance between the first node and the target bicHash
                int newDistance = DhtUtilities.CalculateXorDistance(firstNode.NodeHash, bicHash);

                // Recurse with the new distance and reduced max depth
                return await FindClosestNodeAsync(firstNode.NodeHash, bicHash, maxDepth - 1);
            }

            // If max depth reached, return the first node as the closest
            return DhtResponse<NodeInfo>.Success("Closest node found successfully.", firstNode);
        }
        catch (Exception ex)
        {
            return DhtResponse<NodeInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DhtResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash, int maxDepth = 3)
    {
        var closestNodeResponse = await GetSortedSetClosestNodeAsync(currentNodeHash, bicHash);
        if (closestNodeResponse == null || closestNodeResponse.Data == null)
        {
            return DhtResponse<NodeInfo>.Failure(closestNodeResponse!.Message!);
        }

        return await FindNodeRecursivelyAsync(bicHash, new List<NodeInfo> { closestNodeResponse.Data }, new HashSet<string>(), currentNodeHash, maxDepth);
    }

    public async Task<List<NodeInfo>> GetKClosestNodesAsync(byte[] nodeHash, int k = 20)
    {
        var centralNodeHash = await _hashingService.ByteHash(AppConstants.GetCurrentNodeBIC(_configuration));
        var currentNodehash = await _hashingService.ByteHash(_configuration["NodeConfig:CurrentNode"]!);
        int targetDistance = DhtUtilities.CalculateXorDistance(nodeHash, currentNodehash.Data!);
        var redisBucketsKey = $"dht:buckets{targetDistance}";
        var closestNodes = await _redisDatabase.SortedSetRangeByScoreAsync(redisBucketsKey, targetDistance - k, targetDistance + k, Exclude.None, Order.Ascending, 0, k);
        return closestNodes.Select(nodeData => JsonConvert.DeserializeObject<NodeInfo>(nodeData!)).ToList()!;
    }

    public async Task<List<NodeInfo>> GetKClosestNodesWithAlphaAsync(byte[] nodeHash, int k = 20, int alpha = 3)
    {
        try
        {
            var allNodesResponse = await GetAllNodesAsync("dht:nodes");
            if (allNodesResponse?.Data == null || !allNodesResponse.Data.Any())
            {
                return new List<NodeInfo>();
            }

            // Calculate XOR distances for each node, sort, and take k * alpha closest nodes
            var closestNodes = allNodesResponse.Data!
                .Select(node =>
                {
                    var distance = DhtUtilities.CalculateXorDistance(nodeHash, node.NodeHash);
                    return (Node: node, Distance: distance);
                })
                .OrderBy(pair => pair.Distance)
                .Take(k * alpha)
                .Select(pair => pair.Node)
                .ToList();

            // Retrieve each closest node's peer list from Redis
            var peerListsTasks = closestNodes.Select(async node =>
            {
                try
                {
                    var peersResponse = node.KnownPeers;
                    return peersResponse ?? new List<NodeInfo>();
                }
                catch
                {
                    return new List<NodeInfo>();
                }
            });

            var peerLists = await Task.WhenAll(peerListsTasks);

            // Flatten the peer lists and deduplicate nodes by their hash
            var allNodes = peerLists.SelectMany(peers => peers)
                .Concat(closestNodes) // Include the initial closest nodes as well
                .DistinctBy(node => BitConverter.ToString(node.NodeHash)) // Remove duplicates by node hash
                .OrderBy(node => DhtUtilities.CalculateXorDistance(nodeHash, node.NodeHash)) // Sort by XOR distance
                .Take(k) 
                .ToList();

            return allNodes!;
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Failed to retrieve the k closest nodes.", ex);
        }
    }

    public async Task<DhtResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] accountHash)
    {
        var nodeData = await _redisDatabase.HashGetAsync(key, accountHash);
        if (nodeData.IsNullOrEmpty)
        {
            return DhtResponse<AccountInfo>.Failure("Account not found.");
        }

        var node = JsonConvert.DeserializeObject<AccountInfo>(nodeData!);
        return DhtResponse<AccountInfo>.Success("Account retrieved successfully", node!);
    }

    public async Task<DhtResponse<List<NodeInfo>>> GetAllNodesAsync(string key)
    {
        try
        {
            List<NodeInfo> nodes = new();
            var allNodes = await _redisDatabase.HashGetAllAsync(key);
            foreach (var nodeEntry in allNodes)
            {
                nodes.Add(DeserializeNodeInfo(nodeEntry.Value));
            }

            return nodes.Any()
                ? DhtResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes)
                : DhtResponse<List<NodeInfo>>.Failure("No nodes found", new List<NodeInfo>());
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving all nodes: {ex.Message}", ex);
        }
    }

    public async Task<DhtResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance)
    {
        string bucketKey = $"dht:buckets:{distance}";
        var nodesBiCs = await _redisDatabase.SortedSetRangeByScoreAsync(bucketKey, start: 0, stop: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var nodes = new List<NodeInfo>();
        foreach (var nodeBic in nodesBiCs)
        {
            var jsonData = await _redisDatabase.StringGetAsync(nodeBic.ToString());
            if (!jsonData.IsNull)
            {
                nodes.Add(JsonConvert.DeserializeObject<NodeInfo>(jsonData!)!);
            }
        }

        return DhtResponse<List<NodeInfo>>.Success("Success", nodes);
    }

    public async Task<DhtResponse<long>> GetBucketCountAsync(string bucketKey)
    {
        // Retrieve the count from the counter key
        string countKey = $"{bucketKey}:count";
        long count = (long)await _redisDatabase.StringGetAsync(countKey);
        return DhtResponse<long>.Success("Bucket count retrieved successfully", count);
    }
    
    public async Task<DhtResponse<long>> GetBucketLengthAsync(string bucketKey)
    {
        long count = await _redisDatabase.ListLengthAsync(bucketKey);
        return DhtResponse<long>.Success("Bucket count retrieved successfully", count);
    }

    public async Task<DhtResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey)
    {
        // Get the node with the smallest score (most likely the least recently seen node)
        var leastRecentlySeenNodeHash = await GetLeastRecentlySeenNodeHash(bucketKey);
        if (leastRecentlySeenNodeHash.Data != null && leastRecentlySeenNodeHash.Data!.Length > 0)
        {
            // Step 2: Use the node hash (field) to retrieve the actual NodeInfo object
            RedisValue serializedNodeInfo = await _redisDatabase.HashGetAsync(nodeKey, leastRecentlySeenNodeHash.Data);
            if (!serializedNodeInfo.IsNullOrEmpty)
            {
                var nodeInfo = JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo!);
                return DhtResponse<NodeInfo>.Success("Least recently seen node retrieved", nodeInfo!);
            }
        }

        return DhtResponse<NodeInfo>.Failure("No nodes found in the bucket.");
    }

    public async Task<DhtResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null, bool isCentralNode = false)
    {
        await _redisDatabase.HashSetAsync(key, field, serializedValue);
        if (expiry.HasValue && !isCentralNode)
        {
            await _redisDatabase.KeyExpireAsync(key, expiry);
        }

        return DhtResponse<bool>.Success("Node added/updated successfully", true);
    }

    public async Task<DhtResponse<bool>> SetSortedNodeAsync(string bucketKey, NodeInfo value, double score)
    {
        try
        {
            var serializedValue = SerializeNodeInfo(value);
            string countKey = $"{bucketKey}:count";
            var added = await _redisDatabase.SortedSetAddAsync(bucketKey, serializedValue, score);

            // Increment count in a separate key
            await _redisDatabase.StringIncrementAsync(countKey);

            return added
                ? DhtResponse<bool>.Success("Node added to DHT", true, null, new Dictionary<string, object>() { { "node", value } })
                : DhtResponse<bool>.Success("Failed to Add Node to DHT", true, null, null);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
        {
            throw new InvalidOperationException("Bucket key had an incorrect data type; the key has been reset. Please retry.", ex);
        }
    }

    public async Task<DhtResponse<bool>> SetSortedNodeInListAsync(string bucketKey, NodeInfo value)
    {
        var serializedValue = SerializeNodeInfo(value);
        await _redisDatabase.ListRightPushAsync(bucketKey, serializedValue);

        return DhtResponse<bool>.Success("Node added to DHT", true, null, new Dictionary<string, object> { { "node", value } });
    }

    public async Task<DhtResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, AccountInfo value, double score)
    {
        var serializedValue = SerializeAccountInfo(value);
        await _redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score);
        await _redisDatabase.HashSetAsync(accountKey, value.AccountHash, serializedValue);
        await _redisDatabase.StringSetAsync(value.AccountHash, serializedValue, TimeSpan.FromHours(24));

        return await _redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score)
            ? DhtResponse<bool>.Success("Account added to DHT", true, null, new Dictionary<string, object>() { { "account", value } })
            : DhtResponse<bool>.Success("Failed to Add Account to DHT", true, null, null);
    }

    public async Task<DhtResponse<bool>> RemoveNodeAsync(string key, byte[] field)
    {
        return await _redisDatabase.HashDeleteAsync(key, field)
            ? DhtResponse<bool>.Success("Node removed successfully", true)
            : DhtResponse<bool>.Failure("Node not found", false);
    }

    public async Task<DhtResponse<bool>> RemoveSortedSetNodeAsync(string key, NodeInfo nodeInfo)
    {
        var wasRemoved = await _redisDatabase.SortedSetRemoveAsync(key, JsonConvert.SerializeObject(nodeInfo.NodeHash));
        if (wasRemoved)
        {
            // Optionally, remove the node from the 'nodes' hash if you have one
            await RemoveNodeAsync("dht:nodes", nodeInfo.NodeHash);
            return DhtResponse<bool>.Success("Node removed from DHT", true);
        }
        else
        {
            return DhtResponse<bool>.Success("Node not found in DHT", false);
        }
    }

    public async Task<DhtResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null)
    {
        var transaction = _redisDatabase.CreateTransaction();

        // Watch the key to ensure the transaction only succeeds if the key hasn't changed
        transaction.AddCondition(Condition.KeyExists(bicHash));

        // Queue the update operation in the transaction (update node info in the hash)
        _ = transaction.HashSetAsync("dht:nodes", bicHash, SerializeNodeInfo(nodeInfo));

        if (expiry.HasValue)
        {
            await _redisDatabase.KeyExpireAsync("dht:nodes", expiry);
        }

        return await transaction.ExecuteAsync()
            ? DhtResponse<bool>.Success("Update successful", true)
            : DhtResponse<bool>.Failure("Update failed", false);
    }

    public async Task CleanUpInactiveNodesAsync(string redisNodesKey)
    {
        var dhtNodes = await _redisDatabase.HashGetAllAsync(redisNodesKey);
        foreach (var nodeHash in dhtNodes)
        {
            var nodeInfoResponse = DeserializeNodeInfo(nodeHash.Value);
            if (nodeInfoResponse != null && ShouldEvictNode(nodeInfoResponse))
            {
                await RemoveSortedSetNodeAsync(redisNodesKey, nodeInfoResponse);
            }
        }
    }

    



    #region Private Methods

    private async Task<DhtResponse<byte[]>> GetLeastRecentlySeenNodeHash(string bucketKey)
    {
        try
        {
            var leastRecentlySeenNode = await _redisDatabase.SortedSetRangeByRankAsync(bucketKey, 0, 0);
            if (leastRecentlySeenNode.Length > 0)
            {
                return DhtResponse<byte[]>.Success("Least recently seen node retrieved", leastRecentlySeenNode[0]!);
            }
            return DhtResponse<byte[]>.Failure("No nodes found in the bucket.", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving least recently seen node hash: {ex.Message}", ex);
        }
    }

    private NodeInfo DeserializeNodeInfo(RedisValue serializedNodeInfo)
    {
        return JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo!)!;
    }

    private AccountInfo DeserializeAccountInfo(RedisValue serializedAccountInfo)
    {
        return JsonConvert.DeserializeObject<AccountInfo>(serializedAccountInfo!)!;
    }

    private string SerializeNodeInfo(NodeInfo nodeInfo)
    {
        return JsonConvert.SerializeObject(nodeInfo);
    }

    private string SerializeAccountInfo(AccountInfo accountInfo)
    {
        return JsonConvert.SerializeObject(accountInfo);
    }

    private bool ShouldEvictNode(NodeInfo nodeInfo)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - nodeInfo.LastSeen > 24;
        //return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)nodeInfo.LastSeen > 24;
    }

    private async Task<DhtResponse<NodeInfo>> FindNodeRecursivelyAsync(byte[] bicHash, List<NodeInfo> nodes, HashSet<string> visited, byte[] currentNodeHash, int depth)
    {
        if (depth <= 0 || nodes.Count == 0)
        {
            return DhtResponse<NodeInfo>.Failure("Max depth reached or no nodes to check")!;
        }

        foreach (var node in nodes)
        {
            // Check if we have already visited this node
            if (visited.Contains(BitConverter.ToString(node.NodeHash)))
            {
                continue; // Skip already visited nodes to prevent loops
            }

            visited.Add(BitConverter.ToString(node.NodeHash)); // Mark the node as visited

            // Check if this node is the one we want
            if (node.NodeHash.SequenceEqual(bicHash))
            {
                return DhtResponse<NodeInfo>.Success("Success", node)!;
            }

            // Query known peers if the current node is not the responsible node
            if (node.KnownPeers != null && node.KnownPeers.Any())
            {
                // Recursively search within the known peers of the current node
                var peerNode = await FindNodeRecursivelyAsync(bicHash, node.KnownPeers, visited, currentNodeHash, depth - 1);
                if (peerNode != null)
                {
                    return peerNode;
                }
            }
        }

        return DhtResponse<NodeInfo>.Failure("Failied")!;
    }





    #endregion



}
