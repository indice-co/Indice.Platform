using System.Threading.Tasks;
using IdentityServer4.Stores;

namespace Indice.AspNetCore.Identity.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="IClientStore"/>.
    /// </summary>
    public static class ClientStoreExtensions
    {
        /// <summary>
        /// Determines whether the client is configured to use PKCE.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="clientId">The client identifier.</param>
        /// <returns></returns>
        public static async Task<bool> IsPkceClientAsync(this IClientStore store, string clientId) {
            if (!string.IsNullOrWhiteSpace(clientId)) {
                var client = await store.FindEnabledClientByIdAsync(clientId);
                return client?.RequirePkce == true;
            }

            return false;
        }
    }
}
