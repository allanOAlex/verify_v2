using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Account;

namespace Verify.Application.Dtos.Bank;
public record NodeInfo
{
    public required string NodeBIC { get; init; }
    public required byte[] NodeHash { get; init; }
    public string? NodeEndPoint { get; init; }
    public required Uri NodeUri { get; init; }
    public List<NodeInfo>? KnownPeers { get; init; }
    public List<AccountInfo>? StoredAccounts { get; init; }
    public double LastSeen { get; init; }
    //public DateTimeOffset LastSeen { get; init; }

}
