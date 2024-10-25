using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Account;
public record AccountRequest
{
    public required string InitiatorBIC { get; init; }
    public required string RecipientBIC { get; init; }
    public required string RecipientAccountNumber { get; init; }
    public string? CorrelationId { get; init; }
}
