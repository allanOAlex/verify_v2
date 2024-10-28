namespace Verify.Application.Dtos.Account;
public record AccountResponse
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? OtherNames { get; init; }
    public string? AccountNumber { get; init; }
}
