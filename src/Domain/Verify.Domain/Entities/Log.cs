using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Domain.Entities;
public class Log
{
    [Key]
    public int Id { get; set; }
    public int LogLevel { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset LogDate { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
