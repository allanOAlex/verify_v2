namespace Verify.Application.Dtos.Account;
public record AccountRequest
{
    public required string SenderBic { get; init; }
    public required string RecipientBic { get; init; }
    public required string RecipientAccountNumber { get; init; }
    public string? CorrelationId { get; init; }
}
