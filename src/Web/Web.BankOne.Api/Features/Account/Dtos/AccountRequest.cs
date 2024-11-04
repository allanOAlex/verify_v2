namespace Web.BankOne.Api.Features.Account.Dtos;

public record AccountRequest
{
    public required string SenderBic { get; init; }
    public required string RecipientBic { get; init; }
    public required string RecipientAccountNumber { get; init; }
    public string? CorrelationId { get; init; }
}
