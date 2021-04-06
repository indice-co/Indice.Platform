using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for configuring 'Trusted Device Authorization' feature.
    /// </summary>
    public class TrustedDeviceAuthorizationOptions
    {
        internal IServiceCollection Services { get; set; }
    }
}
