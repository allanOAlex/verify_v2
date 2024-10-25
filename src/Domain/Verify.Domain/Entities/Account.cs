using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Domain.Entities;
public class Account
{
    [Key]
    public int Id { get; set; }
    public string? AccountName { get; set; }
    public string? AccountNumber { get; set; }
    public string? AccountBIC { get; set; }
    public DateTimeOffset DateCreated { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
