using System.ComponentModel.DataAnnotations;

namespace Verify.Domain.Entities;
public class Account
{
    [Key]
    public int Id { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountBic { get; set; }
    public DateTimeOffset DateCreated { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
