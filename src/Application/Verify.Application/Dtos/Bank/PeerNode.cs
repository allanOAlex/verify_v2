using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Bank;

[MessagePackObject]
public record PeerNode
{
    public required string NodeBic { get; init; }
    public required byte[] NodeHash { get; init; }
    public string? NodeEndPoint { get; init; }
    public required Uri NodeUri { get; init; }
    public double LastSeen { get; init; }
}
