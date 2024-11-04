using MessagePack;
using Verify.Application.Dtos.Account;

namespace Verify.Application.Dtos.Bank;


[MessagePackObject]
public record NodeInfo
{
    [Key("NodeBic")]
    public required string NodeBic { get; init; }

    [Key("NodeHash")]
    public required byte[] NodeHash { get; init; }

    [Key("NodeEndPoint")]
    public string? NodeEndPoint { get; init; }

    [Key("NodeUri")]
    public required Uri NodeUri { get; init; }

    [Key("KnownPeers")]
    public List<PeerNode>? KnownPeers { get; init; }

    [Key("Accounts")]
    public List<AccountInfo>? Accounts { get; init; }

    [Key("LastSeen")]
    public double LastSeen { get; init; }

}
