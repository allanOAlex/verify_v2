using StackExchange.Redis;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Application.Abstractions.DHT;
public interface IDhtRedisService
{
    ITransaction CreateTransaction();
    Task<DhtResponse<bool>> NodeExistsAsync(string key, byte[] hash);
    Task<DhtResponse<bool>> SortedSetNodeExistsByScoreAsync(string key, string serializedValue);
    Task<DhtResponse<bool>> SortedSetNodeExistsByRankAsync(string key, byte[] hash);
    Task<DhtResponse<List<NodeInfo>>> GetAllNodesAsync(string key);
    Task<DhtResponse<List<NodeInfo>>> GetNodesByScoreRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on score (e.g. XOR Distance)
    Task<DhtResponse<List<NodeInfo>>> GetNodesByRankRangeAsync(string key, long minRank, long maxRank); // Retrieve nodes based on rank (index-based position in the set)
    Task<DhtResponse<List<NodeInfo>>> GetActiveNodesInBucketAsync(int distance);
    Task<DhtResponse<long>> GetBucketCountAsync(string bucketKey);
    Task<DhtResponse<long>> GetBucketLengthAsync(string bucketKey);
    Task<DhtResponse<NodeInfo>> GetLeastRecentlySeenNodeAsync(string bucketKey, string nodeKey);
    Task<DhtResponse<AccountInfo>> GetAccountNodeAsync(string key, byte[] field);
    Task<DhtResponse<NodeInfo>> GetNodeAsync(string key, byte[] field);
    Task<DhtResponse<NodeInfo>> GetSortedSetClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash);
    Task<DhtResponse<NodeInfo>> GetClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash);
    Task<DhtResponse<NodeInfo>> FindClosestNodeAsync(byte[] currentNodeHash, byte[] bicHash, int maxDepth = 3);
    Task<DhtResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash, int maxDepth = 3);
    Task<List<NodeInfo>> GetKClosestNodesAsync(byte[] nodeHash, int k = 20);
    Task<List<NodeInfo>> GetKClosestNodesWithAlphaAsync(byte[] nodeHash, int k = 20, int alpha = 3);
    Task<DhtResponse<bool>> SetNodeAsync(string key, byte[] field, string serializedValue, TimeSpan? expiry = null, bool isCentralNode = false);

    // Sorted set to store nodes by distance or other criteria
    Task<DhtResponse<bool>> SetSortedNodeAsync(string bucketKey, string serializedValue, double score);
    Task<DhtResponse<bool>> SetSortedNodeInListAsync(string bucketKey, string serializedValue);
    Task<DhtResponse<bool>> SetSortedAccountAsync(string bucketKey, string accountKey, string serializedValue, double score);
    Task<DhtResponse<bool>> RemoveNodeAsync(string key, byte[] field);
    Task<DhtResponse<bool>> RemoveSortedSetNodeAsync(string key, NodeInfo nodeInfo);
    Task<DhtResponse<bool>> UpdateUsingTransaction(byte[] bicHash, NodeInfo nodeInfo, TimeSpan? expiry = null);

    Task CleanUpInactiveNodesAsync(string redisNodesKey);







}
