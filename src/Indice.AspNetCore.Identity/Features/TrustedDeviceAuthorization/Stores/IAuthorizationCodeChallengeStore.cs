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
        Task<string> Create(AuthorizationCode code);
    }
}
