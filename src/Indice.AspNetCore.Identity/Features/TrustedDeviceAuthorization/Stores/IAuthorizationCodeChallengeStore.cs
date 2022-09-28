using System.Threading.Tasks;
using Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Models;

namespace Indice.AspNetCore.Identity.TrustedDeviceAuthorization.Stores
{
    /// <summary>Abstracts the operations used to store an authorization code for a trusted device.</summary>
    internal interface IAuthorizationCodeChallengeStore
    {
        /// <summary>Stores the authorization code.</summary>
        /// <param name="code">The code to store.</param>
        Task<string> GenerateChallenge(TrustedDeviceAuthorizationCode code);
        /// <summary>Retrieves an authorization code by it's key.</summary>
        /// <param name="key">The key to search for.</param>
        Task<TrustedDeviceAuthorizationCode> GetAuthorizationCode(string key);
        /// <summary>Removes an authorization code by it's key.</summary>
        /// <param name="key">The key to delete.</param>
        Task RemoveAuthorizationCode(string key);
    }
}
