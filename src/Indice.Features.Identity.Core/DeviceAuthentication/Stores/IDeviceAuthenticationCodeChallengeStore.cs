using Indice.Features.Identity.Core.DeviceAuthentication.Models;

namespace Indice.Features.Identity.Core.DeviceAuthentication.Stores
{
    /// <summary>Abstracts the operations used to store an authorization code for a device.</summary>
    internal interface IDeviceAuthenticationCodeChallengeStore
    {
        /// <summary>Stores the authorization code.</summary>
        /// <param name="code">The code to store.</param>
        Task<string> GenerateChallenge(DeviceAuthenticationCode code);
        /// <summary>Retrieves an authorization code by it's key.</summary>
        /// <param name="key">The key to search for.</param>
        Task<DeviceAuthenticationCode> GetDeviceAuthenticationCode(string key);
        /// <summary>Removes an authorization code by it's key.</summary>
        /// <param name="key">The key to delete.</param>
        Task RemoveDeviceAuthenticationCode(string key);
    }
}
