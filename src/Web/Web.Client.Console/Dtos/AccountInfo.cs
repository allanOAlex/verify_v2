using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Dtos;
public record AccountInfo
{
    public byte[]? AccountHash { get; init; }
    public string? AccountName { get; init; }
    public string? AccountNumber { get; init; }
    public string? AccountBIC { get; init; }
}
