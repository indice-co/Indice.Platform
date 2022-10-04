using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>Options for configuring 'Trusted Device Authorization' feature.</summary>
    public class TrustedDeviceAuthorizationOptions
    {
        internal IServiceCollection Services { get; set; }
        internal IConfiguration Configuration { get; set; }
    }
}
