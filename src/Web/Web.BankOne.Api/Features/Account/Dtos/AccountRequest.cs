namespace Web.BankOne.Api.Features.Account.Dtos;

public record AccountRequest
{
    public required string InitiatorBIC { get; init; }
    public required string RecipientBIC { get; init; }
    public required string RecipientAccountNumber { get; init; }
    public string? CorrelationId { get; init; }
}
