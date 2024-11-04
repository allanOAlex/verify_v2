using Microsoft.Extensions.Configuration;
using Quartz;
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

            // Create tasks for each node retrieval
            var nodeTasks = nodesBicHashes.Select(async nodesBicHash =>
            {
                var nodeResponse = await _dHtRedisService.GetNodeAsync("dht:nodes", nodesBicHash);
                if (nodeResponse is { Successful: true, Data: not null })
                {
                    return new NodeInfo
                    {
                        NodeBic = nodeResponse.Data.NodeBic,
                        NodeHash = nodeResponse.Data.NodeHash,
                        NodeEndPoint = nodeResponse.Data.NodeEndPoint,
                        NodeUri = nodeResponse.Data.NodeUri,
                        KnownPeers = [],
                        Accounts = []
                    };
                }
                return null;
            });

            // Wait for all tasks to complete and collect successful results
            var nodes = (await Task.WhenAll(nodeTasks)).Where(node => node != null).ToList();

            //var nodes = new List<NodeInfo>();
            //foreach (var nodesBicHash in nodesBicHashes)
            //{
            //    var nodeResponse = await _dHtRedisService.GetNodeAsync("dht:nodes", nodesBicHash);
            //    if (nodeResponse is { Successful: true, Data: not null })
            //    {
            //        nodes.Add(new NodeInfo
            //        {
            //            NodeBic = nodeResponse.Data.NodeBic,
            //            NodeHash = nodeResponse.Data.NodeHash,
            //            NodeEndPoint = nodeResponse.Data.NodeEndPoint,
            //            NodeUri = nodeResponse.Data.NodeUri,
            //            KnownPeers = [],
            //            Accounts = []
            //        });
            //    }
            //}

            if (nodes.Any())
            {
                await AddNodeToPeers(nodes!, _configuration["NodeConfig:CurrentNode"]!, senderBic!, recipientBic!);
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
        catch (Exception)
        {
            throw;
        }
    }

    private async Task<DhtResponse<bool>> AddNodeToPeers_(List<NodeInfo> nodes, string centralNodeId, string senderBic, string recipientBic)
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

            var centralPeer = centralNode != null ? new PeerNode
            {
                NodeBic = centralNode.NodeBic,
                NodeHash = centralNode.NodeHash,
                NodeEndPoint = centralNode.NodeEndPoint,
                NodeUri = centralNode.NodeUri,
                LastSeen = centralNode.LastSeen,
            } : null;

            var senderPeer = senderNode != null ? new PeerNode
            {
                NodeBic = senderNode.NodeBic,
                NodeHash = senderNode.NodeHash,
                NodeEndPoint = senderNode.NodeEndPoint,
                NodeUri = senderNode.NodeUri,
                LastSeen = senderNode.LastSeen,
            } : null;

            var recipientPeer = recipientNode != null ? new PeerNode
            {
                NodeBic = recipientNode.NodeBic,
                NodeHash = recipientNode.NodeHash,
                NodeEndPoint = recipientNode.NodeEndPoint,
                NodeUri = recipientNode.NodeUri,
                LastSeen = recipientNode.LastSeen,
            } : null;

            AddPeerIfMissing(centralNode!, senderPeer!);
            AddPeerIfMissing(centralNode!, recipientPeer!);
            AddPeerIfMissing(senderNode!, centralPeer!);
            AddPeerIfMissing(senderNode!, recipientPeer!);
            AddPeerIfMissing(recipientNode!, centralPeer!);
            AddPeerIfMissing(recipientNode!, senderPeer!);

            var updateTasks = new List<Task>();
            if (recipientNode != null)
            {
                updateTasks.Add(_nodeManagementService.AddOrUpdateNodeAsync(recipientNode, false));
            }
            if (centralNode != null)
            {
                updateTasks.Add(_nodeManagementService.AddOrUpdateNodeAsync(centralNode, false));
            }
            if (senderNode != null)
            {
                updateTasks.Add(_nodeManagementService.AddOrUpdateNodeAsync(senderNode, false));
            }

            await Task.WhenAll(updateTasks);

            return DhtResponse<bool>.Success("Successfully added nodes to peers", true);
        }
        catch (Exception)
        {
            throw;
        }
    }

    private static void AddPeerIfMissing(NodeInfo node, PeerNode peer)
    {
        if (node == null)
        {
            return;
        }

        if (node != null && node.KnownPeers != null && !node.KnownPeers.Any(p => p.NodeBic.SequenceEqual(peer.NodeBic)))
        {
            node.KnownPeers.Add(peer);
        }
    }

}
