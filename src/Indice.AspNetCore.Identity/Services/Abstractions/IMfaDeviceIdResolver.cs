using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity
{
    /// <summary>An abstraction used to specify the way to resolve the device identifier used for MFA.</summary>
    public interface IMfaDeviceIdResolver
    {
        /// <summary>Gets the device identifier.</summary>
        Task<string> Resolve();
    }
}
