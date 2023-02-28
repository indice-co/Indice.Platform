using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Options for configuring 'Device Authentication' feature.</summary>
public class DeviceAuthenticationOptions
{
    internal IServiceCollection Services { get; set; }
    internal IConfiguration Configuration { get; set; }
}
