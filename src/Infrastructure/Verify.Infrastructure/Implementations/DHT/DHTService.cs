using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

using Refit;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Utilities.DHT;
using Verify.Infrastructure.Utilities.DHT.ApiClients;

namespace Verify.Infrastructure.Implementations.DHT;
internal sealed class DhtService : IDhtService
{
    private readonly IApiClientFactory _apiClientFactory;
    private readonly IConfiguration _configuration;
    private readonly IHashingService _hashingService;
    private readonly INodeManagementService _nodeManagementService;
    private readonly IDhtRedisService _dHtRedisService;


    public DhtService(
        IHttpClientFactory httpClientFactory, 
        IApiClientFactory apiClientFactory,
        IConfiguration configuration,
        IHashingService hashingService,
        INodeManagementService nodeManagementService,
        IDhtRedisService dhtRedisService)
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
        _apiClientFactory = apiClientFactory;
        _configuration = configuration;
        _hashingService = hashingService;
        _nodeManagementService = nodeManagementService;
        _dHtRedisService = dhtRedisService;

    }


    public async Task<DhtResponse<AccountInfo>> FetchAccountData(AccountRequest accountRequest)
    {
        var accountResponse = await LookupAccountInMemoryAsync(accountRequest);
        if (accountResponse.Successful)
            return accountResponse;

        var senderBicHashResponse = await _hashingService.ByteHash(accountRequest.SenderBic);
        var senderBicHash = senderBicHashResponse.Data ?? [];

        var recipientBicHashResponse = await _hashingService.ByteHash(accountRequest.RecipientBic);
        var recipientBicHash = recipientBicHashResponse.Data ?? [];

        // Parallel checks for existence
        var senderExistsTask = _dHtRedisService.NodeExistsAsync("dht:nodes", senderBicHash);
        var recipientExistsTask = _dHtRedisService.NodeExistsAsync("dht:nodes", recipientBicHash);

        var senderExists = await senderExistsTask;
        var recipientExists = await recipientExistsTask;

        // Collect tasks for adding nodes if they don't exist
        var addNodeTasks = new List<Task<DhtResponse<bool>>>();

        if (!senderExists.Data)
        {
            var addInitiatorTask = AddNodeToDhtAsync(accountRequest.SenderBic, senderBicHash);
            addNodeTasks.Add(addInitiatorTask);
        }

        if (!recipientExists.Data)
        {
            var addRecipientTask = AddNodeToDhtAsync(accountRequest.RecipientBic, recipientBicHash);
            addNodeTasks.Add(addRecipientTask);
        }

        // Run all add tasks in parallel if there are nodes to add
        if (addNodeTasks.Count > 0)
        {
            var addNodeResponses = await Task.WhenAll(addNodeTasks);
            if (addNodeResponses.Any(response => !response.Successful))
            {
                return DhtResponse<AccountInfo>.Failure("Failed to add one or more nodes to the DHT.");
            }

            var currentNodeHashResponse = await _hashingService.ByteHash(_configuration["NodeConfig:CurrentNode"]!);
            var nodesBicHashes = new List<byte[]>
            {
                senderBicHash,
                recipientBicHash,
                currentNodeHashResponse.Data!
            };

                
            List<NodeInfo> nodes = [];
            foreach (var nodesBicHash in nodesBicHashes)
            {
                var nodeResponse = await _dHtRedisService.GetNodeAsync("dht:nodes", nodesBicHash);
                if (nodeResponse is { Successful: true, Data: not null })
                {
                    nodes.Add(nodeResponse.Data);
                }
            }

            if (nodes.Any())
            {
               await AddNodeToPeers(nodes, currentNodeHashResponse.Data!, senderBicHash, recipientBicHash);
            }
                
        }

        var currentNodeHash = await _hashingService.ByteHash(_configuration["NodeConfig:CurrentNode"]!);

        var responsibleNodeResponse = await FindClosestResponsibleNodeAsync(currentNodeHash.Data!, recipientBicHash);
        if (!responsibleNodeResponse.Successful)
        {
            return DhtResponse<AccountInfo>.Failure(responsibleNodeResponse.Message!);
        }

        var responsibleNode = responsibleNodeResponse.Data;
        if (string.IsNullOrEmpty(responsibleNode!.NodeUri.Scheme) || string.IsNullOrEmpty(responsibleNode.NodeUri.Host) || responsibleNode.NodeUri.Port <= 0)
        {
            return DhtResponse<AccountInfo>.Failure("Invalid node URI components. Cannot construct query URL.");
        }
        var queryUrl = $"{responsibleNode.NodeUri.Scheme}://{responsibleNode.NodeUri.Host}:{responsibleNode.NodeUri.Port}/";
        var accountDataResponse = await QueryBankAsync(queryUrl, accountRequest);
        if (!accountDataResponse.Successful)
        {
            return DhtResponse<AccountInfo>.Failure("Failed to retrieve account details from the responsible node.");
        }

        await StoreAccountDataAsync(accountDataResponse.Data!);

        return DhtResponse<AccountInfo>.Success("Account data fetched successfully.", accountDataResponse.Data!);
    }

    public async Task<DhtResponse<AccountInfo>> LookupAccountInMemoryAsync(AccountRequest accountRequest)
    {
        var accountHash = await _hashingService.ByteHash(accountRequest.RecipientAccountNumber);
        var accountDataResponse = await _dHtRedisService.GetAccountNodeAsync("dht:accounts", accountHash.Data!);
        if (accountDataResponse.Data == null)
        {
            return DhtResponse<AccountInfo>.Failure("Account not found.");
        }

        return DhtResponse<AccountInfo>.Success("Account found", accountDataResponse.Data!);
    }

    public async Task<DhtResponse<NodeInfo>> FindClosestResponsibleNodeAsync(byte[] currentNodeHash, byte[] bicHash)
    {
        return await _dHtRedisService.FindClosestResponsibleNodeAsync(currentNodeHash, bicHash);
    }

    private async Task<List<NodeInfo>> GetKClosestNodesAsync(byte[] nodeHash, int k = 20)
    {
        // Retrieve all nodes from Redis (local node's routing table)
        var allNodesResponse = await _dHtRedisService.GetAllNodesAsync("dht:nodes");
        if (!allNodesResponse.Data!.Any())
        {
            return new List<NodeInfo>();
        }

        // Calculate XOR distance for each node and sort by closest
        var closestNodes = allNodesResponse.Data!
            .Select(nodeEntry =>
            {
                var nodeInfo = nodeEntry;
                var distance = DhtUtilities.CalculateXorDistance(nodeHash, nodeInfo.NodeHash);
                return (Node: nodeInfo, Distance: distance);
            })
            .OrderBy(pair => pair.Distance)
            .Take(k)
            .Select(pair => pair.Node)
            .ToList();

        return closestNodes;
    }

    public async Task<DhtResponse<AccountInfo>> QueryBankAsync(string queryUrl, AccountRequest accountRequest)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(queryUrl))
            {
                return DhtResponse<AccountInfo>.Failure("Bank base URL is invalid.");
            }

            // Create a Refit client for the specified bank
            var bankApiClient = _apiClientFactory.CreateClient(queryUrl);
            var accountDetailsResponse = await bankApiClient.FetchAccountData(accountRequest);

            AccountInfo accountInfo = new()
            {
                AccountHash = [],
                AccountBic = accountRequest.RecipientBic,
                AccountName = $"{accountDetailsResponse.FirstName} {accountDetailsResponse.LastName} {accountDetailsResponse.OtherNames}",
                AccountNumber = accountDetailsResponse.AccountNumber
            };

            return DhtResponse<AccountInfo>.Success("Account data retrieved successfully", accountInfo);
        }
        catch (ApiException apiEx)
        {
            // Capture specific HTTP errors
            return DhtResponse<AccountInfo>.Failure($"API error occurred: {apiEx.StatusCode}, Message: {apiEx.Content}");
        }
        catch (HttpRequestException httpEx)
        {
            // Handle network issues (e.g., connection failure, timeouts)
            return DhtResponse<AccountInfo>.Failure($"Network error occurred: {httpEx.Message}");
        }
        catch (Exception ex)
        {
            return DhtResponse<AccountInfo>.Failure($"An error occurred: {ex.Message}");
        }
    }

    public async Task<DhtResponse<AccountInfo>> StoreAccountDataAsync(AccountInfo accountInfo)
    {
        var accountHashResponse = await _hashingService.ByteHash(accountInfo.AccountNumber!);
        await _dHtRedisService.SetNodeAsync("dht:accounts", accountHashResponse.Data!, JsonConvert.SerializeObject(accountInfo), TimeSpan.FromHours(24));
        return DhtResponse<AccountInfo>.Success(
            "Account data stored successfully.",
            new AccountInfo
            {
                AccountHash = accountInfo.AccountHash,
                AccountBic = accountInfo.AccountBic,
                AccountNumber = accountInfo.AccountNumber,
                AccountName = accountInfo.AccountName
            }
        );
    }

    public async Task<DhtResponse<bool>> AddNodeToPeers(List<NodeInfo>? nodes, byte[] centralNodeHash, byte[] senderBicHash, byte[] recipinetBicHash)
    {
        try
        {
            if (nodes == null || nodes.Count == 0)
            {
                return DhtResponse<bool>.Failure("No nodes provided to add to peers.");
            }

            var centralNode = nodes.FirstOrDefault(n => n.NodeBic == _configuration["NodeConfig:CurrentNode"]);
            var senderNode = nodes.FirstOrDefault(n => n.NodeHash == senderBicHash);
            var recipientNode = nodes.FirstOrDefault(n => n.NodeHash == recipinetBicHash);

            if (centralNode == null && senderNode == null && recipientNode == null)
            {
                return DhtResponse<bool>.Failure("No peers found to add.");
            }

            if (centralNode != null)
            {
                // Add the Central Node to Sender's peers if it doesn't already exist
                if (senderNode != null && !senderNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(centralNode.NodeHash)))
                {
                    senderNode.KnownPeers!.Add(centralNode);
                }

                // Add the Central Node to Recipient's peers if it doesn't already exist
                if (recipientNode != null && !recipientNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(centralNode.NodeHash)))
                {
                    recipientNode.KnownPeers!.Add(centralNode);
                }
            }

            if (senderNode != null)
            {
                // Add the Sender Node to Recipient's peers if it doesn't already exist
                if (recipientNode != null && !recipientNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(senderNode.NodeHash)))
                {
                    recipientNode.KnownPeers!.Add(senderNode);
                }

                // Add the Sender Node to Central's peers if it doesn't already exist
                if (centralNode != null && !centralNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(senderNode.NodeHash)))
                {
                    centralNode.KnownPeers!.Add(senderNode);
                }
            }

            if (recipientNode != null)
            {
                // Add the Recipient Node to Sender's peers if it doesn't already exist
                if (senderNode != null && !senderNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(recipientNode.NodeHash)))
                {
                    senderNode.KnownPeers!.Add(recipientNode);
                }

                // Add the Recipient Node to Central's peers if it doesn't already exist
                if (centralNode != null && !centralNode.KnownPeers!.Any(peer => peer.NodeHash.SequenceEqual(recipientNode.NodeHash)))
                {
                    centralNode.KnownPeers!.Add(recipientNode);
                }
            }

            // Persist changes back to the DHT
            if (centralNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(centralNode, false);
            }

            if (senderNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(senderNode, false);
            }

            if (recipientNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(recipientNode, false);
            }

            return DhtResponse<bool>.Success("Successfully added nodes to peers", true);
        }
        catch (Exception ex)
        {
            // Log the exception for debugging purposes
            return DhtResponse<bool>.Failure($"An error occurred while adding peers: {ex.Message}");
        }
    }


    #region Private Methods

    private async Task<DhtResponse<bool>> AddNodeToDhtAsync(string bic, byte[] bicHash)
    {
        var nodeEndpointResponse = await _nodeManagementService.GetNodeEndpointFromConfigAsync(bic);
        if (!nodeEndpointResponse.Successful)
        {
            return DhtResponse<bool>.Failure(nodeEndpointResponse.Message!);
        }

        NodeInfo nodeToAdd = new()
        {
            NodeBic = bic,
            NodeHash = bicHash,
            NodeEndPoint = nodeEndpointResponse.Data,
            NodeUri = new Uri(nodeEndpointResponse.Data!),
            LastSeen = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        return await _nodeManagementService.AddOrUpdateNodeAsync(nodeToAdd);
    }


    #endregion




}
