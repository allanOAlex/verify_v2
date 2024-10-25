using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Web.Client.Console.Configurations;

internal class AppSettings
{
    public string? ApiBaseUrl { get; set; }
    public int TimeoutSeconds { get; set; }
}
