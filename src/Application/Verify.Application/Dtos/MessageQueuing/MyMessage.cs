using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.MessageQueuing;
public record MyMessage
{
    public string? Text { get; init; }
}
