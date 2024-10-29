using Microsoft.Extensions.Configuration;

namespace Verify.Shared.Utilities;

public static class AppConstants
{

    public static string GetCurrentNodeBIC(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(nameof(configuration));
        return configuration["NodeConfig:CurrentNode"]!;
    }
}
