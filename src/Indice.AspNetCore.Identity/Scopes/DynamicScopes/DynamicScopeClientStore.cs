using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Dynamic scopes Decorator for <see cref="IClientStore"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DynamicScopeClientStore<T> : IClientStore where T : IClientStore
    {

        private readonly T _innerStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicScopeClientStore{T}"/> class.
        /// </summary>
        /// <param name="inner">The decorated client store</param>
        public DynamicScopeClientStore(T inner) {
            _innerStore = inner;
        }

        /// <inheritdoc />
        public async Task<Client> FindClientByIdAsync(string clientId) {
            var client = await _innerStore.FindClientByIdAsync(clientId);
            if (client != null && !(client.AllowedScopes is DynamicScopeNameCollection)) {
                // initialize the collection if not initialized yet;
                client.AllowedScopes = new DynamicScopeNameCollection(client.AllowedScopes);
            }
            return client;
        }
    }
}