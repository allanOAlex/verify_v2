using Verify.Application.Dtos.Bank;

namespace Verify.Application.Dtos.DHT;

public record Bucket
{
    public int Index { get; init; }
    public List<NodeInfo>? Nodes { get; init; }
}
