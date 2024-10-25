using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Verify.Infrastructure.Configurations.Caching;
public class ElastiCacheSetting
{
    public string? Endpoint { get; set; }
    public string? AccessKey { get; set; }
    public string? SecretKey { get; set; }
    public string? Region { get; set; }
}
