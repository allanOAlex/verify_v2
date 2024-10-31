using MessagePack;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Quartz;
using Refit;
using System;
using System.Text.Json;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.IServices;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;
using Verify.Infrastructure.Implementations.Caching;
using Verify.Infrastructure.Implementations.DHT.Jobs;
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
    private readonly IScheduler _scheduler;
    private readonly ICacheService _cacheService;


    public DhtService(
        IHttpClientFactory httpClientFactory, 
        IApiClientFactory apiClientFactory,
        IConfiguration configuration,
        IHashingService hashingService,
        INodeManagementService nodeManagementService,
        IDhtRedisService dhtRedisService,
        ISchedulerFactory schedulerFactory,
        ICacheService cacheService
        )
    {
        var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(100);
        _apiClientFactory = apiClientFactory;

        _scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

        _hashingService = hashingService ?? throw new ArgumentNullException(nameof(hashingService));
        _nodeManagementService = nodeManagementService ?? throw new ArgumentNullException(nameof(nodeManagementService));
        _dHtRedisService = dhtRedisService ?? throw new ArgumentNullException(nameof(dhtRedisService));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    }


    public async Task<DhtResponse<AccountInfo>> FetchAccountData_(AccountRequest accountRequest)
    {
        try
        {
            var accountResponse = await LookupAccountInMemoryAsync(accountRequest);
            if (accountResponse.Successful) return accountResponse;

            var queryUrlResponse = _nodeManagementService.GetNodeEndpointFromConfigAsync(accountRequest.RecipientBic);
            var queryUrlResponseUri = new Uri(queryUrlResponse.Data!);
            var queryUrl = $"{queryUrlResponseUri.Scheme}://{queryUrlResponseUri.Host}:{queryUrlResponseUri.Port}/";

            // Check if queryUrl has a valid scheme and port
            if (!Uri.TryCreate(queryUrl, UriKind.Absolute, out var uri) || string.IsNullOrEmpty(uri.Scheme) || uri.Port == -1)
            {
                return DhtResponse<AccountInfo>.Failure("Invalid URL: The query URL is missing a scheme or port.");
            }

            var accountDataResponse = await QueryBankAsync(queryUrl, accountRequest);
            if (!accountDataResponse.Successful)
            {
                return DhtResponse<AccountInfo>.Failure("Failed to retrieve account details from the responsible node.");
            }

            var accountHashResponse = await _hashingService.ByteHash(accountDataResponse.Data!.AccountNumber!);

            var jobDataMap = new JobDataMap
            {
                ["AccountHash"] = accountHashResponse.Data!,
                ["SerializedAccountInfo"] = MessagePackSerializer.Serialize(accountDataResponse.Data)
                //["SerializedAccountInfo"] = JsonSerializer.Serialize(accountDataResponse.Data)
            };

            string jobKey = $"StoreAccountDataJob:{accountHashResponse.Data}:{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}";
            var storeAccountJobKey = new JobKey(jobKey);
            if (!await _scheduler.CheckExists(storeAccountJobKey))
            {
                IJobDetail jobDetail = JobBuilder
                    .Create<StoreAccountDataJob>()
                    .WithIdentity(storeAccountJobKey)
                    .StoreDurably() // we need to store durably if no trigger is associated
                    .WithDescription("Store-AccountData-Job")
                    .Build();

                await _scheduler.AddJob(jobDetail, true);
            }

            await _scheduler.TriggerJob(storeAccountJobKey, jobDataMap);

            return DhtResponse<AccountInfo>.Success("Account data fetched successfully.", accountDataResponse.Data!);
        }
        catch (Exception)
        {

            throw;
        }
        
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

        if (addNodeTasks.Count > 0)
        {
            var addNodeResponses = await Task.WhenAll(addNodeTasks);
            if (addNodeResponses.Any(response => !response.Successful))
            {
                return DhtResponse<AccountInfo>.Failure($"Failed to add one or more nodes to the DHT:");
            }

            var currentNodeHashResponse = await _hashingService.ByteHash(_configuration["NodeConfig:CurrentNode"]!);
            var nodesBicHashes = new List<byte[]>
                {
                    senderBicHash,
                    recipientBicHash,
                    currentNodeHashResponse.Data!
                };

            var addPeersbDataMap = new JobDataMap
            {
                ["NodesBicHashes"] = nodesBicHashes,
                ["SenderBic"] = accountRequest.SenderBic,
                ["RecipientBic"] = accountRequest.RecipientBic
            };

            var addPeersJobKey = new JobKey("AddNodeToPeersJob");
            if (!await _scheduler.CheckExists(addPeersJobKey))
            {
                // Define the job only if it hasn't been added to Quartz
                IJobDetail jobDetail = JobBuilder
                    .Create<AddNodeToPeersJob>()
                    .WithIdentity(addPeersJobKey)
                    .StoreDurably() // we need to store durably if no trigger is associated
                    .WithDescription("Add-Node-ToPeers-Job")
                    .Build();

                await _scheduler.AddJob(jobDetail, true);
            }

            await _scheduler.TriggerJob(addPeersJobKey, addPeersbDataMap);

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

        // Store account data

        var accountHashResponse = await _hashingService.ByteHash(accountDataResponse.Data!.AccountNumber!);
        var accountHash = accountHashResponse.Data;
        var jobDataMap = new JobDataMap
        {
            ["AccountHash"] = accountHash!,
            ["SerializedAccountInfo"] = MessagePackSerializer.Serialize(accountDataResponse.Data)
            //["SerializedAccountInfo"] = JsonSerializer.Serialize(accountDataResponse.Data)
        };

        var storeAccountJobKey = new JobKey("StoreAccountDataJob");
        if (!await _scheduler.CheckExists(storeAccountJobKey))
        {
            IJobDetail jobDetail = JobBuilder
                .Create<StoreAccountDataJob>()
                .WithIdentity(storeAccountJobKey)
                .StoreDurably() // we need to store durably if no trigger is associated
                .WithDescription("Store-AccountData-Job")
                .Build();

            await _scheduler.AddJob(jobDetail, true);
        }

        await _scheduler.TriggerJob(storeAccountJobKey, jobDataMap);

        return DhtResponse<AccountInfo>.Success("Account data fetched successfully.", accountDataResponse.Data!);
    }

    public async Task<DhtResponse<AccountInfo>> _FetchAccountData(AccountRequest accountRequest)
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

        if (addNodeTasks.Count > 0)
        {
            var addNodeResponses = await Task.WhenAll(addNodeTasks);
            if (addNodeResponses.Any(response => !response.Successful))
            {
                return DhtResponse<AccountInfo>.Failure($"Failed to add one or more nodes to the DHT:");
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
                    NodeInfo node = new()
                    {
                        NodeBic = nodeResponse.Data.NodeBic,
                        NodeHash = nodeResponse.Data.NodeHash,
                        NodeEndPoint = nodeResponse.Data.NodeEndPoint,
                        NodeUri = nodeResponse.Data.NodeUri,
                        KnownPeers = [],
                        Accounts = [],

                    };

                    nodes.Add(node);
                }
            }

            if (nodes.Any())
            {
                await AddNodeToPeers(nodes, _configuration["NodeConfig:CurrentNode"]!, accountRequest.SenderBic, accountRequest.RecipientBic);
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
        await _dHtRedisService.SetNodeByteValueAsync($"dht:accounts", accountHashResponse.Data!, MessagePackSerializer.Serialize(accountInfo), TimeSpan.FromHours(24));
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

    public async Task<DhtResponse<bool>> AddNodeToPeers(List<NodeInfo>? nodes, string centralNodeId, string senderBic, string recipinetBic)
    {
        try
        {
            if (nodes == null || nodes.Count == 0)
            {
                return DhtResponse<bool>.Failure("No nodes provided to add to peers.");
            }

            var centralNode = nodes.FirstOrDefault(n => n.NodeBic == _configuration["NodeConfig:CurrentNode"]);
            var senderNode = nodes.FirstOrDefault(n => n.NodeBic == senderBic);
            var recipientNode = nodes.FirstOrDefault(n => n.NodeBic == recipinetBic);

            if (centralNode == null && senderNode == null && recipientNode == null)
            {
                return DhtResponse<bool>.Failure("No peers found to add.");
            }

            PeerNode peerNode;
            if (centralNode != null)
            {
                peerNode = new()
                {
                    NodeBic = centralNode.NodeBic,
                    NodeHash = centralNode.NodeHash,
                    NodeEndPoint = centralNode.NodeEndPoint,
                    NodeUri = centralNode.NodeUri,
                    LastSeen = centralNode.LastSeen,
                };

                if (senderNode != null && senderNode.KnownPeers != null && !senderNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    senderNode.KnownPeers.Add(peerNode);
                }

                if (recipientNode != null && recipientNode.KnownPeers != null && !recipientNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(centralNode!.NodeBic)))
                {
                    recipientNode.KnownPeers.Add(peerNode!);
                }
            }

            if (senderNode != null)
            {
                peerNode = new()
                {
                    NodeBic = senderNode.NodeBic,
                    NodeHash = senderNode.NodeHash,
                    NodeEndPoint = senderNode.NodeEndPoint,
                    NodeUri = senderNode.NodeUri,
                    LastSeen = senderNode.LastSeen,
                };

                if (centralNode != null && centralNode.KnownPeers != null && !centralNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    centralNode.KnownPeers.Add(peerNode);
                }

                if (recipientNode != null && recipientNode.KnownPeers != null && !recipientNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    recipientNode.KnownPeers.Add(peerNode);
                }
            }

            if (recipientNode != null)
            {
                peerNode = new()
                {
                    NodeBic = recipientNode.NodeBic,
                    NodeHash = recipientNode.NodeHash,
                    NodeEndPoint = recipientNode.NodeEndPoint,
                    NodeUri = recipientNode.NodeUri,
                    LastSeen = recipientNode.LastSeen,
                };

                if (centralNode != null && centralNode.KnownPeers != null && !centralNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(recipientNode.NodeBic)))
                {
                    centralNode.KnownPeers.Add(peerNode);
                }

                if (senderNode != null && senderNode.KnownPeers != null && !senderNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(recipientNode.NodeBic)))
                {
                    senderNode.KnownPeers.Add(peerNode);
                }
            }

            //Persist the data
            if (recipientNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(recipientNode!, false);
            }

            if (centralNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(centralNode!, false);
            }

            if (senderNode != null)
            {
                await _nodeManagementService.AddOrUpdateNodeAsync(senderNode!, false);
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
        var nodeEndpointResponse = _nodeManagementService.GetNodeEndpointFromConfigAsync(bic);
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
