namespace Verify.Application.Dtos.Account;
public record AccountInfo
{
    public byte[]? AccountHash { get; init; }
    public string? AccountName { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountBic { get; init; }
}
