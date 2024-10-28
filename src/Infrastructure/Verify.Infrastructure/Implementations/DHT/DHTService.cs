using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

using Refit;

using StackExchange.Redis;

using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.Interfaces;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Infrastructure.Utilities.DHT.ApiClients;
using Verify.Shared.Exceptions;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DHTService : IDHTService
{
    private readonly HttpClient httpClient;
    private readonly IApiClientFactory apiClientFactory;
    private readonly IConfiguration configuration;
    private readonly IHashingService hashingService;
    private readonly INodeManagementService nodeManagementService;
    private readonly IDHTRedisService dHTRedisService;


    public DHTService(
        IHttpClientFactory httpClientFactory, 
        IApiClientFactory ApiClientFactory,
        IConfiguration Configuration,
        IHashingService HashingService,
        INodeManagementService NodeManagementService,
        IDHTRedisService DHTRedisService)
    {
        httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
        apiClientFactory = ApiClientFactory;
        configuration = Configuration;
        hashingService = HashingService;
        nodeManagementService = NodeManagementService;
        dHTRedisService = DHTRedisService;

    }


    public async Task<DHTResponse<AccountInfo>> FetchAccountData(AccountRequest accountRequest)
    {
        try
        {
            var accountResponse = await LookupAccountInMemoryAsync(accountRequest);
            if (accountResponse.Successful)
                return accountResponse;

            var senderBicHashResponse = await hashingService.ByteHash(accountRequest.SenderBIC);
            var senderBicHash = senderBicHashResponse.Data ?? Array.Empty<byte>();

            var recipientBicHashResponse = await hashingService.ByteHash(accountRequest.RecipientBIC);
            var recipientBicHash = senderBicHashResponse.Data ?? Array.Empty<byte>();

            // Parallel checks for existence
            var senderExistsTask = dHTRedisService.NodeExistsAsync("dht:nodes", senderBicHash);
            var recipientExistsTask = dHTRedisService.NodeExistsAsync("dht:nodes", recipientBicHash);

            var senderExists = await senderExistsTask;
            var recipientExists = await recipientExistsTask;

            // Collect tasks for adding nodes if they don't exist
            var addNodeTasks = new List<Task<DHTResponse<bool>>>();

            if (!senderExists.Data)
            {
                var addInitiatorTask = AddNodeToDHTAsync(accountRequest.SenderBIC, senderBicHash);
                addNodeTasks.Add(addInitiatorTask);
            }

            if (!recipientExists.Data)
            {
                var addRecipientTask = AddNodeToDHTAsync(accountRequest.RecipientBIC, recipientBicHash);
                addNodeTasks.Add(addRecipientTask);
            }

            // Run all add tasks in parallel if there are nodes to add
            if (addNodeTasks.Count > 0)
            {
                var addNodeResponses = await Task.WhenAll(addNodeTasks);

                // Handle failure in any add task
                foreach (var response in addNodeResponses)
                {
                    if (!response.Successful)
                    {
                        return DHTResponse<AccountInfo>.Failure("Failed to add one or more nodes to the DHT.");
                    }
                }
            }

            //return DHTResponse<AccountInfo>.Success("Nodes verified and added if necessary", null);


            //var nodeExistsInDHTResponse = await dHTRedisService.NodeExistsAsync("dht:nodes", senderBicHash);
            //if (!nodeExistsInDHTResponse.Data)
            //{
            //    var nodeEndpointResponse = await nodeManagementService.GetNodeEndpointFromConfigAsync(accountRequest.SenderBIC);
            //    if (!nodeEndpointResponse.Successful)
            //    {
            //        //ToDo: Decide how to handle this case; here we return a failure response
            //        return DHTResponse<AccountInfo>.Failure(nodeEndpointResponse.Message!);
            //    }

            //    NodeInfo nodeToAdd = new()
            //    {
            //        NodeBIC = accountRequest.SenderBIC,
            //        NodeHash = senderBicHash,
            //        NodeEndPoint = nodeEndpointResponse.Data,
            //        NodeUri = new Uri(nodeEndpointResponse.Data!),
            //        LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //    };

            //    // ToDo: BUG! BUG! BUG! - NodeInfo (Current Node) is the node we are trying to add; we eed to have a current node to compare the distance
            //    var addNodeResponse = await nodeManagementService.AddOrUpdateNodeAsync(nodeToAdd, true);
            //    if (!addNodeResponse.Successful)
            //    {
            //        //ToDo: Decide how to handle this case; here we return a failure response
            //        return DHTResponse<AccountInfo>.Failure("Failed to add or update the node in the DHT.");
            //    }
            //}

            // Route the request using Kademlia’s routing algorithm to find the responsible node
            var accountHash = await hashingService.ByteHash(accountRequest.RecipientAccountNumber);
            var currentNodeHash = await hashingService.ByteHash(configuration["NodeConfig:CurrentNode"]!);
            var responsibleNodeResponse = await FindClosestResponsibleNodeAsync(currentNodeHash.Data!, senderBicHash);
            if (!responsibleNodeResponse.Successful)
            {
                return DHTResponse<AccountInfo>.Failure(responsibleNodeResponse.Message!);
            }

            var responsibleNode = responsibleNodeResponse.Data;
            var queryUrl = $"{responsibleNode!.NodeUri.Scheme}://{responsibleNode.NodeUri.Host}:{responsibleNode.NodeUri.Port}/";
            var accountDataResponse = await QueryBankAsync(queryUrl, accountRequest);
            if (!accountDataResponse.Successful)
            {
                return DHTResponse<AccountInfo>.Failure("Failed to retrieve account details from the responsible node.");
            }

            var storeDataResponse = await StoreAccountDataAsync(accountDataResponse.Data!);
            return DHTResponse<AccountInfo>.Success("Account data fetched successfully.", accountDataResponse.Data!);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<AccountInfo>> LookupAccountInMemoryAsync(AccountRequest accountRequest)
    {
        try
        {
            var accountHash = await hashingService.ByteHash(accountRequest.RecipientAccountNumber);

            // ToDo: Use correct method
            var accountDataResponse = await dHTRedisService.GetAccountNodeAsync("dht:accounts", accountHash.Data!);
            if (accountDataResponse.Data == null)
            {
                return DHTResponse<AccountInfo>.Failure("Account not found.");
            }

            return DHTResponse<AccountInfo>.Success("Account found", accountDataResponse.Data!);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash)
    {
        try
        {
            return await dHTRedisService.GetSortedSetClosestNodeAsync(currentNodeHash, bicHash);
        }
        catch (Exception)
        {

            throw;
        }
    }

    public async Task<DHTResponse<NodeInfo>> GetClosestNode(byte[] bicHash)
    {
        try
        {
            var allNodes = await dHTRedisService.GetAllNodesAsync("dht:nodes");

            // Filter nodes to only include those with the same bicHash
            var relevantNodes = allNodes.Data?.Where(node => node.NodeHash.SequenceEqual(bicHash)).ToList();
            if (relevantNodes == null || !relevantNodes.Any())
            {
                return DHTResponse<NodeInfo>.Failure("No nodes found for the given BIC hash.");
            }

            NodeInfo? closestNode = null;
            long closestDistance = long.MaxValue;

            Parallel.ForEach(allNodes.Data!, node =>
            {
                var distance = DHTUtilities.CalculateXorDistance(bicHash, node!.NodeHash);

                // Use Interlocked.CompareExchange for thread-safe closest node update
                if (distance < Interlocked.CompareExchange(ref closestDistance, distance, closestDistance))
                {
                    closestNode = node;
                }
            });

            return closestNode != null
                ? DHTResponse<NodeInfo>.Success("Success", closestNode)
                : DHTResponse<NodeInfo>.Failure("No nodes found in the DHT.", null);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<List<NodeInfo>> GetKClosestNodesAsync(byte[] nodeHash, int k = 20)
    {
        try
        {
            // Retrieve all nodes from Redis (local node's routing table)
            var allNodesResponse = await dHTRedisService.GetAllNodesAsync("dht:nodes");
            if (!allNodesResponse.Data!.Any())
            {
                return new List<NodeInfo>();
            }

            // Calculate XOR distance for each node and sort by closest
            var closestNodes = allNodesResponse.Data!
                .Select(nodeEntry =>
                {
                    var nodeInfo = nodeEntry;
                    var distance = DHTUtilities.CalculateXorDistance(nodeHash, nodeInfo!.NodeHash);
                    return (Node: nodeInfo, Distance: distance);
                })
                .OrderBy(pair => pair.Distance)
                .Take(k)
                .Select(pair => pair.Node)
                .ToList();

            return closestNodes!;
        }
        catch (Exception)
        {

            throw;
        }

    }

    public async Task<DHTResponse<bool>> NodeHasDataForKeyAsync(NodeInfo closestNode, byte[] nodeHash)
    {
        try
        {
            var hasData = await dHTRedisService.NodeExistsAsync("dht:nodes", nodeHash);
            return DHTResponse<bool>.Success("Check completed", hasData.Data);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task<DHTResponse<AccountInfo>> QueryBankAsync(string queryUrl, AccountRequest accountRequest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(queryUrl))
            {
                return DHTResponse<AccountInfo>.Failure("Bank base URL is invalid.");
            }

            // Create a Refit client for the specified bank
            var bankApiClient = apiClientFactory.CreateClient(queryUrl);
            var accountDetailsResponse = await bankApiClient.FetchAccountData(accountRequest);

            AccountInfo accountInfo = new()
            {
                AccountHash = Array.Empty<byte>(),
                AccountBIC = accountRequest.RecipientBIC,
                AccountName = $"{accountDetailsResponse.FirstName} {accountDetailsResponse.LastName} {accountDetailsResponse.OtherNames}",
                AccountNumber = accountDetailsResponse.AccountNumber
            };

            return accountDetailsResponse != null
                ? DHTResponse<AccountInfo>.Success("Account data retrieved successfully", accountInfo)
                : DHTResponse<AccountInfo>.Failure("Account data could not be retrieved.");
        }
        catch (ApiException apiEx)
        {
            // Capture specific HTTP errors
            return DHTResponse<AccountInfo>.Failure($"API error occurred: {apiEx.StatusCode}, Message: {apiEx.Content}");
        }
        catch (HttpRequestException httpEx)
        {
            // Handle network issues (e.g., connection failure, timeouts)
            return DHTResponse<AccountInfo>.Failure($"Network error occurred: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            return DHTResponse<AccountInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DHTResponse<AccountInfo>> StoreAccountDataAsync(AccountInfo accountInfo)
    {
        try
        {
            var accountHashResponse = await hashingService.ByteHash(accountInfo.AccountNumber!);
            await dHTRedisService.SetNodeAsync("dht:accounts", accountHashResponse.Data!, JsonConvert.SerializeObject(accountInfo), TimeSpan.FromHours(24));
            return DHTResponse<AccountInfo>.Success(
                "Account data stored successfully.",
                new AccountInfo
                {
                    AccountHash = accountInfo.AccountHash,
                    AccountBIC = accountInfo.AccountBIC,
                    AccountNumber = accountInfo.AccountNumber,
                    AccountName = accountInfo.AccountName,
                }!
            );

        }
        catch (Exception)
        {

            throw;
        }
    }

    private async Task<DHTResponse<bool>> AddNodeToDHTAsync(string bic, byte[] bicHash)
    {
        var nodeEndpointResponse = await nodeManagementService.GetNodeEndpointFromConfigAsync(bic);
        if (!nodeEndpointResponse.Successful)
        {
            return DHTResponse<bool>.Failure(nodeEndpointResponse.Message!);
        }
    
        NodeInfo nodeToAdd = new()
        {
            NodeBIC = bic,
            NodeHash = bicHash,
            NodeEndPoint = nodeEndpointResponse.Data,
            NodeUri = new Uri(nodeEndpointResponse.Data!),
            LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
    
        return await nodeManagementService.AddOrUpdateNodeAsync(nodeToAdd, true);
    }

    public async Task<DHTResponse<bool>> AddNodeToPeers(NodeInfo nodeInfo)
    {
        try
        {
            var nodeHash = await hashingService.ByteHash(nodeInfo.NodeBIC);
            var closestNodes = await GetKClosestNodesAsync(nodeHash.Data!);
            foreach (var node in closestNodes)
            {
                if (!nodeInfo.KnownPeers!.Contains(node))
                {
                    nodeInfo.KnownPeers.Add(node);
                }
            }

            return DHTResponse<bool>.Success("Node Added to Peers", true);
        }
        catch (Exception)
        {

            throw;
        }
    }


}
