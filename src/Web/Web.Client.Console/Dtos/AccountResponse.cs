using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Dtos;
public record AccountResponse
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? OtherNames { get; init; }
    public string? AccountNumber { get; init; }

}
