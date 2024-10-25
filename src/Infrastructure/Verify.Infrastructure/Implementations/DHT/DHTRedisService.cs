using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;
using Verify.Infrastructure.Utilities.DHT;

using static MassTransit.ValidationResultExtensions;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DHTRedisService : IDHTRedisService
{
    private readonly IDatabase redisDatabase;

    public DHTRedisService(IDatabase RedisDatabase)
    {
        redisDatabase = RedisDatabase;
            
    }


    public async Task<DHTResponse<bool>> NodeExistsAsync(string key, byte[] hash)
    {
        try
        {
            bool exists = await redisDatabase.HashExistsAsync(key, hash);
            string message = exists
                ? $"Node {Convert.ToBase64String(hash)} exists"
                : $"Node {Convert.ToBase64String(hash)} does not exist";

            return exists
                ? DHTResponse<bool>.Success(message, true)
                : DHTResponse<bool>.Failure(message, false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<bool>> SortedSetNodeExistsByScoreAsync(string key, string serializedValue)
    {
        try
        {
            var exists = await redisDatabase.SortedSetScoreAsync(key, serializedValue);
            return exists.HasValue
                ? DHTResponse<bool>.Success("Node exists", true)
                : DHTResponse<bool>.Failure("Node does not exist", false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<bool>> SortedSetNodeExistsByRankAsync(string key, byte[] hash)
    {
        try
        {
            var exists = await redisDatabase.SortedSetRankAsync(key, JsonConvert.SerializeObject(hash));
            return exists.HasValue
                ? DHTResponse<bool>.Success("Node exists", true)
                : DHTResponse<bool>.Failure("Node does not exist", false);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error checking node existence: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field)
    {
        try
        {
            var nodeData = await redisDatabase.HashGetAsync(key, field);
            if (nodeData.IsNullOrEmpty)
            {
                return DHTResponse<NodeInfo>.Failure("Node not found.");
            }

            var node = JsonConvert.DeserializeObject<NodeInfo>(nodeData!);
            return DHTResponse<NodeInfo>.Success("Node retrieved successfully", node!);
        }
        catch (JsonException jsonEx)
        {
            throw new Exception("Error deserializing the node information from Redis.", jsonEx);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodeList = new();
            var nodes = await redisDatabase.SortedSetRangeByScoreAsync(key, minScore, maxScore);
            foreach (var node in nodes)
            {
                var serializedNodeInfo = await redisDatabase.HashGetAsync(key, node);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodeList.Add(DeserializeNodeInfo(serializedNodeInfo));
                }
            }

            return nodes.Any()
                 ? DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodeList)
                 : DHTResponse<List<NodeInfo>>.Failure("No nodes found in the range", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving nodes by score range: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetNodesByRankRangeAsync(string key, long minScore, long maxScore)
    {
        try
        {
            List<NodeInfo> nodeList = new();
            var nodes = await redisDatabase.SortedSetRangeByRankAsync(key, minScore, maxScore);
            foreach (var node in nodes)
            {
                var serializedNodeInfo = await redisDatabase.HashGetAsync(key, node);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    nodeList.Add(DeserializeNodeInfo(serializedNodeInfo));
                }
            }

            return nodes.Any()
                 ? DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodeList)
                 : DHTResponse<List<NodeInfo>>.Failure("No nodes found in the range", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving nodes by score range: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetSortedSetClosestNodeAsync(byte[] bicHash)
    {
        try
        {
            // Calculate the bucket key based on the bicHash
            int distance = DHTUtilities.CalculateXorDistance(bicHash, bicHash); // This can stay if needed for bucket logic
            string redisBucketsKey = $"dht:buckets:{distance}"; //dht

            // Fetch nodes from the sorted set that match the bicHash
            var matchingNodes = await redisDatabase.SortedSetRangeByScoreAsync(redisBucketsKey, 0, double.MaxValue, Exclude.None, Order.Ascending);

            // Filter to include only those nodes with the same bicHash
            var filteredNodes = matchingNodes
                .Select(nodeData => JsonConvert.DeserializeObject<NodeInfo>(nodeData!))
                .Where(node => node!.NodeHash.SequenceEqual(bicHash))
                .ToList();

            if (filteredNodes == null || !filteredNodes.Any())
            {
                return DHTResponse<NodeInfo>.Failure("No nodes found for the given BIC hash.");
            }

            NodeInfo closestNode = null!;
            long closestDistance = long.MaxValue;

            Parallel.ForEach(filteredNodes, node =>
            {
                var currentDistance = DHTUtilities.CalculateXorDistance(bicHash, node!.NodeHash);

                // Interlocked to safely update the closest node
                if (currentDistance < Interlocked.CompareExchange(ref closestDistance, currentDistance, closestDistance))
                {
                    // Store the closest node in a thread-safe manner
                    NodeInfo currentClosestNode = node;

                    // This might cause issues because `closestNode` is not thread-safe
                    // We can use Interlocked to manage this safely
                    Interlocked.Exchange(ref closestNode, currentClosestNode);
                }
            });

            return closestNode != null
                ? DHTResponse<NodeInfo>.Success("Closest node found successfully.", closestNode)
                : DHTResponse<NodeInfo>.Failure("No closest node found in the DHT.", null);
        }
        catch (Exception ex)
        {
            return DHTResponse<NodeInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DHTResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] accountHash)
    {
        try
        {
            var nodeData = await redisDatabase.HashGetAsync(key, accountHash);
            if (nodeData.IsNullOrEmpty)
            {
                return DHTResponse<AccountInfo>.Failure("Account not found.");
            }

            var node = JsonConvert.DeserializeObject<AccountInfo>(nodeData!);
            return DHTResponse<AccountInfo>.Success("Account retrieved successfully", node!);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync(string key)
    {
        try
        {
            List<NodeInfo> nodes = new();
            var allNodes = await redisDatabase.HashGetAllAsync(key);
            foreach (var nodeEntry in allNodes)
            {
                nodes.Add(DeserializeNodeInfo(nodeEntry.Value));
            }

            return nodes.Any()
                ? DHTResponse<List<NodeInfo>>.Success("Nodes retrieved successfully", nodes)
                : DHTResponse<List<NodeInfo>>.Failure("No nodes found", new List<NodeInfo>());
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving all nodes: {ex.Message}", ex);
        }
    }

    public async Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance)
    {
        try
        {
            string bucketKey = $"dht:buckets:{distance}";
            var nodesBICs = await redisDatabase.SortedSetRangeByScoreAsync(bucketKey, start: 0, stop: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var nodes = new List<NodeInfo>();
            foreach (var nodeBIC in nodesBICs)
            {
                var jsonData = await redisDatabase.StringGetAsync(nodeBIC.ToString());
                if (!jsonData.IsNull)
                {
                    nodes.Add(JsonConvert.DeserializeObject<NodeInfo>(jsonData!)!);
                }
            }

            return DHTResponse<List<NodeInfo>>.Success("Success", nodes);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<long>> GetBucketCountAsync(string bucketKey, StorageType storageType)
    {
        try
        {
            if (storageType == StorageType.Redis)
            {
                // Retrieve the count from the counter key
                string countKey = $"{bucketKey}:count";
                long count = (long)await redisDatabase.StringGetAsync(countKey);
                return DHTResponse<long>.Success("Bucket count retrieved successfully", count);
            }

            //ToDo: Get Count From InMemeory
            return DHTResponse<long>.Success("Bucket count retrieved successfully", 0);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
        {
            // Handle case where key type is wrong in Redis
            throw new InvalidOperationException("Bucket key exists with an incorrect data type.", ex);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<long>> GetBucketCountAsync(string bucketKey)
    {
        try
        {
            // Retrieve the count from the counter key
            string countKey = $"{bucketKey}:count";
            long count = (long)await redisDatabase.StringGetAsync(countKey);
            return DHTResponse<long>.Success("Bucket count retrieved successfully", count);
        }
        catch (Exception)
        {
            throw;
        }
    }
    
    public async Task<DHTResponse<long>> GetBucketLengthAsync(string bucketKey)
    {
        try
        {
            long count = await redisDatabase.ListLengthAsync(bucketKey);
            return DHTResponse<long>.Success("Bucket count retrieved successfully", count);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey)
    {
        try
        {
            // Get the node with the smallest score (most likely the least recently seen node)
            var leastRecentlySeenNodeHash = await GetLeastRecentlySeenNodeHash(bucketKey);
            if (leastRecentlySeenNodeHash.Data!.Length > 0)
            {
                // Step 2: Use the node hash (field) to retrieve the actual NodeInfo object
                RedisValue serializedNodeInfo = await redisDatabase.HashGetAsync(nodeKey, leastRecentlySeenNodeHash.Data);
                if (!serializedNodeInfo.IsNullOrEmpty)
                {
                    var nodeInfo = JsonConvert.DeserializeObject<NodeInfo>(serializedNodeInfo!);
                    return DHTResponse<NodeInfo>.Success("Least recently seen node retrieved", nodeInfo!);
                }

                return DHTResponse<NodeInfo>.Failure("Node info not found in the hash.");
            }

            return DHTResponse<NodeInfo>.Failure("No nodes found in the bucket.");
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null)
    {
        try
        {
            await redisDatabase.HashSetAsync(key, field, serializedValue);
            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync(key, expiry);
            }

            return DHTResponse<bool>.Success("Node added/updated successfully", true);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetSortedNodeAsync(string bucketKey, NodeInfo value, double score)
    {
        try
        {
            var serializedValue = SerializeNodeInfo(value);
            string countKey = $"{bucketKey}:count";
            var added = await redisDatabase.SortedSetAddAsync(bucketKey, serializedValue, score);

            // Increment count in a separate key
            await redisDatabase.StringIncrementAsync(countKey);

            return added
                ? DHTResponse<bool>.Success("Node added to DHT", true, null, new Dictionary<string, object>() { { "node", value } })
                : DHTResponse<bool>.Success("Failed to Add Node to DHT", true, null, null);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
        {
            throw new InvalidOperationException("Bucket key had an incorrect data type; the key has been reset. Please retry.", ex);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetSortedNodeInListAsync(string bucketKey, NodeInfo value)
    {
        try
        {
            var serializedValue = SerializeNodeInfo(value);
            await redisDatabase.ListRightPushAsync(bucketKey, serializedValue);

            return DHTResponse<bool>.Success("Node added to DHT", true, null, new Dictionary<string, object> { { "node", value } });
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, AccountInfo value, double score)
    {
        try
        {
            var serializedValue = SerializeAccountInfo(value);
            await redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score);
            await redisDatabase.HashSetAsync(accountKey, value.AccountHash, serializedValue);
            await redisDatabase.StringSetAsync(value.AccountHash, serializedValue, TimeSpan.FromHours(24));

            return await redisDatabase.SortedSetAddAsync(bucketKey, value.AccountHash, score)
                ? DHTResponse<bool>.Success("Account added to DHT", true, null, new Dictionary<string, object>() { { "account", value } })
                : DHTResponse<bool>.Success("Failed to Add Account to DHT", true, null, null);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> RemoveNodeAsync(string key, byte[] field)
    {
        try
        {
            return await redisDatabase.HashDeleteAsync(key, field)
            ? DHTResponse<bool>.Success("Node removed successfully", true)
            : DHTResponse<bool>.Failure("Node not found", false);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<bool>> RemoveSortedSetNodeAsync(string key, NodeInfo nodeInfo)
    {
        try
        {
            var wasRemoved = await redisDatabase.SortedSetRemoveAsync(key, JsonConvert.SerializeObject(nodeInfo.NodeHash));
            if (wasRemoved)
            {
                // Optionally, remove the node from the 'nodes' hash if you have one
                await RemoveNodeAsync("dht:nodes", nodeInfo.NodeHash);
                return DHTResponse<bool>.Success("Node removed from DHT", true);
            }
            else
            {
                return DHTResponse<bool>.Success("Node not found in DHT", false);
            }
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task UpdateNodeTimestampAsync(string bucketKey, byte[] nodeHash)
    {
        double currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        await redisDatabase.SortedSetAddAsync(bucketKey, nodeHash, currentTimestamp);
    }

    public async Task<DHTResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null)
    {
        try
        {
            var transaction = redisDatabase.CreateTransaction();

            // Watch the key to ensure the transaction only succeeds if the key hasn't changed
            transaction.AddCondition(Condition.KeyExists(bicHash));

            // Queue the update operation in the transaction (update node info in the hash)
            _ = transaction.HashSetAsync("dht:nodes", bicHash, SerializeNodeInfo(nodeInfo));

            if (expiry.HasValue)
            {
                await redisDatabase.KeyExpireAsync("dht:nodes", expiry);
            }

            return await transaction.ExecuteAsync()
                ? DHTResponse<bool>.Success("Update successful", true)
                : DHTResponse<bool>.Failure("Update failed", false);

        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task CleanUpInactiveNodesAsync(string redisNodesKey)
    {
        var dhtNodes = await redisDatabase.HashGetAllAsync(redisNodesKey);
        foreach (var nodeHash in dhtNodes)
        {
            var nodeInfoResponse = DeserializeNodeInfo(nodeHash.Value);
            if (nodeInfoResponse != null && ShouldEvictNode(nodeInfoResponse))
            {
                await RemoveSortedSetNodeAsync(redisNodesKey, nodeInfoResponse);
            }
        }
    }

    private bool ShouldEvictNode(NodeInfo nodeInfo)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - nodeInfo.LastSeen > 24;
        //return DateTimeOffset.UtcNow.ToUnixTimeSeconds() - (long)nodeInfo.LastSeen > 24;
    }



    #region Private Methods

    private async Task<DHTResponse<byte[]>> GetLeastRecentlySeenNodeHash(string bucketKey)
    {
        try
        {
            var leastRecentlySeenNode = await redisDatabase.SortedSetRangeByRankAsync(bucketKey, 0, 0);
            if (leastRecentlySeenNode.Length > 0)
            {
                return DHTResponse<byte[]>.Success("Least recently seen node retrieved", leastRecentlySeenNode[0]!);
            }
            return DHTResponse<byte[]>.Failure("No nodes found in the bucket.", null);
        }
        catch (Exception ex)
        {
            throw new ApplicationException($"Error retrieving least recently seen node hash: {ex.Message}", ex);
        }
    }

    private async Task EvictInactiveOrLeastRecentlySeenNodeAsync(string bucketKey)
    {
        // Step 1: Fetch the node with the oldest timestamp (least recently seen)
        var leastRecentlySeenNode = await redisDatabase.SortedSetRangeByRankWithScoresAsync(bucketKey, 0, 0);

        if (leastRecentlySeenNode.Length > 0)
        {
            var nodeHash = leastRecentlySeenNode[0].Element;

            // Step 2: Check node's last active timestamp
            var nodeTimestamp = leastRecentlySeenNode[0].Score;  // Assuming score is the timestamp
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Step 3: If node is inactive (hasn't been seen for a threshold), evict it
            var inactiveThreshold = TimeSpan.FromHours(24).TotalSeconds;  // Define your own inactivity threshold
            if (currentTime - nodeTimestamp >= inactiveThreshold)
            {
                // Node is inactive, remove it
                await redisDatabase.SortedSetRemoveAsync(bucketKey, nodeHash);
                await redisDatabase.HashDeleteAsync("nodes", nodeHash);
            }
            else
            {
                // If no inactive nodes, evict the least recently seen
                await redisDatabase.SortedSetRemoveAsync(bucketKey, nodeHash);
            }
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

    

    #endregion



}
