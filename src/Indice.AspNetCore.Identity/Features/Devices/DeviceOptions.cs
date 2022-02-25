using Microsoft.Extensions.DependencyInjection;

namespace Indice.AspNetCore.Identity.Api
{
    /// <summary>
    /// Options used to configure <b>Devices</b> feature.
    /// </summary>
    public class DeviceOptions 
    {
        internal IServiceCollection Services { get; set; }
    }
}
