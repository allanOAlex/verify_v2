using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Bank;

public record AddNodeRequest
{
    public int BankId { get; init; }
    public required string BankBIC { get; init; }
    public required byte[] BankHash { get; init; }
    public required Uri BankUri { get; init; }
    public required string BankEndPoint { get; init; }

}
