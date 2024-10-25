using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Application.Dtos.Common;
public record SearchRequest
{
    public string? SearchParam { get; init; }
    public string? SearchValue { get; init; } = null;
    public PaginationSetting? PaginationSetting { get; init; } = new();
}
