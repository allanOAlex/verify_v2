using MessagePack;

namespace Web.Client.Blazor.Dtos;

[MessagePackObject]
public class AccountRequest
{
    [Key("SenderBic")]
    public required string SenderBic { get; init; }

    [Key("RecipientBic")]
    public required string RecipientBic { get; init; }

    [Key("RecipientAccountNumber")]
    public required string RecipientAccountNumber { get; init; }
}
