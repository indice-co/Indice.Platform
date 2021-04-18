using System.Threading.Tasks;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Abstracts the operations used to store an authorization code for a trusted device.
    /// </summary>
    internal interface IAuthorizationCodeChallengeStore
    {
        /// <summary>
        /// Stores the authorization code.
        /// </summary>
        /// <param name="code">The code to store.</param>
        Task<string> GenerateChallenge(AuthorizationCode code);
        /// <summary>
        /// Retrieves an authorization code by it's key.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        Task<AuthorizationCode> GetAuthorizationCode(string key);
        /// <summary>
        /// Removes an authorization code by it's key.
        /// </summary>
        /// <param name="key">The key to delete.</param>
        Task RemoveAuthorizationCode(string key);
    }
}
