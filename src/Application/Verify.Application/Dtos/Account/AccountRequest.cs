using MessagePack;

namespace Verify.Application.Dtos.Account;

[MessagePackObject]
public record AccountRequest
{
    [Key("SenderBic")]
    public required string SenderBic { get; init; }

    [Key("RecipientBic")]
    public required string RecipientBic { get; init; }

    [Key("RecipientAccountNumber")]
    public required string RecipientAccountNumber { get; init; }

    [Key("CorrelationId")]
    public string? CorrelationId { get; init; }
}
