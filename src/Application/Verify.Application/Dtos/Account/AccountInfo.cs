using MessagePack;

namespace Verify.Application.Dtos.Account;

[MessagePackObject(keyAsPropertyName: true)]
public record AccountInfo
{
    public byte[]? AccountHash { get; init; }
    public string? AccountName { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountBic { get; init; }
}
