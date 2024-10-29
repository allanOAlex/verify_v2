using Microsoft.Extensions.Configuration;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verify.Application.Abstractions.DHT;
using Verify.Application.Abstractions.DHT.Jobs;
using Verify.Application.Dtos.Account;
using Verify.Application.Dtos.Bank;
using Verify.Application.Dtos.Common;

namespace Verify.Infrastructure.Implementations.DHT.Jobs;
internal sealed class AddNodeToPeersJob : IAddNodeToPeersJob
{
    private readonly IDhtRedisService _dHtRedisService;
    private readonly INodeManagementService _nodeManagementService;
    private readonly IHashingService _hashingService;
    private readonly IConfiguration _configuration;

    public AddNodeToPeersJob(
        IDhtRedisService dHtRedisService,
        INodeManagementService nodeManagementService,
        IHashingService hashingService,
        IConfiguration configuration)
    {
        _dHtRedisService = dHtRedisService;
        _nodeManagementService = nodeManagementService;
        _hashingService = hashingService;
        _configuration = configuration;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var nodesBicHashes = context.MergedJobDataMap["NodesBicHashes"] as List<byte[]> ?? new List<byte[]>();
            var senderBic = context.MergedJobDataMap["SenderBic"] as string;
            var recipientBic = context.MergedJobDataMap["RecipientBic"] as string;

            var nodes = new List<NodeInfo>();
            foreach (var nodesBicHash in nodesBicHashes)
            {
                var nodeResponse = await _dHtRedisService.GetNodeAsync("dht:nodes", nodesBicHash);
                if (nodeResponse is { Successful: true, Data: not null })
                {
                    nodes.Add(new NodeInfo
                    {
                        NodeBic = nodeResponse.Data.NodeBic,
                        NodeHash = nodeResponse.Data.NodeHash,
                        NodeEndPoint = nodeResponse.Data.NodeEndPoint,
                        NodeUri = nodeResponse.Data.NodeUri,
                        KnownPeers = new List<NodeInfo>(),
                        Accounts = new List<AccountInfo>()
                    });
                }
            }

            if (nodes.Any())
            {
                await AddNodeToPeers(nodes, _configuration["NodeConfig:CurrentNode"]!, senderBic!, recipientBic!);
            }
        }
        catch (Exception)
        {

            throw;
        }
        
    }

    private async Task<DhtResponse<bool>> AddNodeToPeers(List<NodeInfo> nodes, string centralNodeId, string senderBic, string recipientBic)
    {
        try
        {
            if (nodes == null || nodes.Count == 0)
            {
                return DhtResponse<bool>.Failure("No nodes provided to add to peers.");
            }

            var centralNode = nodes.FirstOrDefault(n => n.NodeBic == _configuration["NodeConfig:CurrentNode"]);
            var senderNode = nodes.FirstOrDefault(n => n.NodeBic == senderBic);
            var recipientNode = nodes.FirstOrDefault(n => n.NodeBic == recipientBic);

            if (centralNode == null && senderNode == null && recipientNode == null)
            {
                return DhtResponse<bool>.Failure("No peers found to add.");
            }

            if (centralNode != null)
            {
                if (senderNode != null && senderNode.KnownPeers != null && !senderNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    senderNode.KnownPeers.Add(senderNode);
                }

                if (recipientNode != null && recipientNode.KnownPeers != null && !recipientNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode!.NodeBic)))
                {
                    recipientNode.KnownPeers.Add(senderNode!);
                }

                await _nodeManagementService.AddOrUpdateNodeAsync(centralNode, false);
            }

            if (senderNode != null)
            {

                if (centralNode != null && centralNode.KnownPeers != null && !centralNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    centralNode.KnownPeers.Add(senderNode);
                }

                if (recipientNode != null && recipientNode.KnownPeers != null && !recipientNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(senderNode.NodeBic)))
                {
                    recipientNode.KnownPeers.Add(senderNode);
                }

                await _nodeManagementService.AddOrUpdateNodeAsync(senderNode, false);
            }

            if (recipientNode != null)
            {
                if (centralNode != null && centralNode.KnownPeers != null && !centralNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(recipientNode.NodeBic)))
                {
                    centralNode.KnownPeers.Add(recipientNode);
                }

                if (senderNode != null && senderNode.KnownPeers != null && !senderNode.KnownPeers.Any(peer => peer.NodeBic.SequenceEqual(recipientNode.NodeBic)))
                {
                    senderNode.KnownPeers.Add(recipientNode);
                }

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

}
