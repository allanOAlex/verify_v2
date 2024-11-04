namespace Web.Client.Console.Dtos;
public record AccountInfo
{
    public byte[]? AccountHash { get; init; }
    public string? AccountName { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountBic { get; init; }
}
