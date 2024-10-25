using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verify.Application.Dtos.Bank;

namespace Verify.Application.Dtos.DHT;

public record Bucket
{
    public int Index { get; init; }
    public List<NodeInfo>? Nodes { get; init; }
}
