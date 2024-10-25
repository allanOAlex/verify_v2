using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Dtos;
public record AccountRequest
{
    public required string InitiatorBIC { get; init; }
    public required string RecipientBIC { get; init; }
    public required string RecipientAccountNumber { get; init; }
}
