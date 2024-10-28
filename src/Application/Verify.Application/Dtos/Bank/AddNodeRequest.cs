namespace Verify.Application.Dtos.Bank;

public record AddNodeRequest
{
    public int BankId { get; init; }
    public required string BankBic { get; init; }
    public required byte[] BankHash { get; init; }
    public required Uri BankUri { get; init; }
    public required string BankEndPoint { get; init; }

}
