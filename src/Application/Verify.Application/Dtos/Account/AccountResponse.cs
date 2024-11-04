using MessagePack;

namespace Verify.Application.Dtos.Account;

[MessagePackObject]
public record AccountResponse
{
    [Key("FirstName")]
    public string? FirstName { get; init; }

    [Key("LastName")]
    public string? LastName { get; init; }

    [Key("OtherNames")]
    public string? OtherNames { get; init; }

    [Key("AccountNumber")]
    public string? AccountNumber { get; init; }
}
