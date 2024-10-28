using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using StackExchange.Redis;

using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Domain.Enums;

namespace Verify.Application.Abstractions.DHT;
public interface IDHTRedisService
{
    Task<DHTResponse<bool>> NodeExistsAsync(string key, byte[] hash);
    Task<DHTResponse<bool>> SortedSetNodeExistsByScoreAsync(string key, string serializedValue);
    Task<DHTResponse<bool>> SortedSetNodeExistsByRankAsync(string key, byte[] hash);
    Task<DHTResponse<List<NodeInfo>>> GetAllNodesAsync(string key);
    Task<DHTResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on score (e.g. XOR Distance)
    Task<DHTResponse<List<NodeInfo>>> GetNodesByRankRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on rank (index-based position in the set)
    Task<DHTResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance);
    Task<DHTResponse<long>> GetBucketCountAsync(string key, StorageType storageType);
    Task<DHTResponse<long>> GetBucketCountAsync(string bucketKey);
    Task<DHTResponse<long>> GetBucketLengthAsync(string bucketKey);
    Task<DHTResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey);
    Task<DHTResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] field);
    Task<DHTResponse<NodeInfo>> GetNodeAsync(string key, byte[] field);
    Task<DHTResponse<NodeInfo>> GetSortedSetClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash);
    Task<DHTResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null, bool isCentralNode = false);

    // Sorted set to store nodes by distance or other criteria
    Task<DHTResponse<bool>> SetSortedNodeAsync(string bucketKey, NodeInfo value, double score);
    Task<DHTResponse<bool>> SetSortedNodeInListAsync(string bucketKey, NodeInfo value);
    Task<DHTResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, AccountInfo value, double score);
    Task<DHTResponse<bool>> RemoveNodeAsync(string key, byte[] field);
    Task<DHTResponse<bool>> RemoveSortedSetNodeAsync(string key, NodeInfo nodeInfo);
    Task UpdateNodeTimestampAsync(string bucketKey, byte[] nodeHash);
    Task<DHTResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null);

    Task CleanUpInactiveNodesAsync(string redisNodesKey);







}
