namespace Web.BankOne.Api.Features.Account.Dtos;

public record AccountResponse
{
    public string? AccountId { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? OtherNames { get; init; }
    public string? AccountNumber { get; init; }
}
