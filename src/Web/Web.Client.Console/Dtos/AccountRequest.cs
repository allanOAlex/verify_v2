namespace Web.Client.Console.Dtos;
public record AccountRequest
{
    public required string SenderBic { get; init; }
    public required string RecipientBic { get; init; }
    public required string RecipientAccountNumber { get; init; }
}
