using Verify.Application.Dtos.Account;

namespace Verify.Application.Dtos.Bank;

public record NodeInfo
{
    public required string NodeBic { get; init; }
    public required byte[] NodeHash { get; init; }
    public string? NodeEndPoint { get; init; }
    public required Uri NodeUri { get; init; }
    public List<NodeInfo>? KnownPeers { get; init; }
    public List<AccountInfo>? Accounts { get; init; }
    public double LastSeen { get; init; }

}
