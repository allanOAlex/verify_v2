using MessagePack;

namespace Web.Client.Blazor.Dtos;

[MessagePackObject]
public record AccountInfo
{
    [Key("AccountHash")]
    public byte[]? AccountHash { get; init; }

    [Key("AccountName")]
    public string? AccountName { get; init; }

    [Key("AccountNumber")]
    public string? AccountNumber { get; init; }

    [Key("AccountBic")]
    public string? AccountBic { get; init; }
}
