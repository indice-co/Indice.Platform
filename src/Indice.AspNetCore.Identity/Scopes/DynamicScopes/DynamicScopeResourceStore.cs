using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Indice.AspNetCore.Identity.Scopes
{
    /// <summary>
    /// Dynamic scopes decorator for <see cref="IResourceStore"/>.
    /// </summary>
    /// <typeparam name="T">The type of store.</typeparam>
    internal class DynamicScopeResourceStore<T> : IResourceStore where T : IResourceStore
    {
        private readonly IResourceStore _inner;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicScopeResourceStore{T}" /> class.
        /// </summary>
        public DynamicScopeResourceStore(T inner) {
            _inner = inner;
        }

        /// <inheritdoc/>
        public Task<Resources> GetAllResourcesAsync() => _inner.GetAllResourcesAsync();

        /// <inheritdoc/>
        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames) {
            return _inner.FindIdentityResourcesByScopeNameAsync(scopeNames);
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames) {
            return _inner.FindApiScopesByNameAsync(scopeNames);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames) {
            if (scopeNames == null) {
                throw new ArgumentNullException(nameof(scopeNames));
            }
            var resources = await GetAllResourcesAsync();
            var apis = new List<ApiResource>();
            foreach (var api in resources.ApiResources) {
                var matches = api.Scopes.Select(scopeName => new ApiScope(scopeName)).FindMatches(scopeNames);
                if (!matches.Any()) {
                    continue;
                }
                var apiClone = new ApiResource {
                    Enabled = api.Enabled,
                    Name = api.Name,
                    DisplayName = api.DisplayName,
                    ApiSecrets = api.ApiSecrets,
                    Scopes = new HashSet<string>(matches.Select(apiScope => apiScope.Name)),
                    UserClaims = api.UserClaims
                };
                apis.Add(apiClone);
            }
            return apis;
        }

        /// <inheritdoc/>
        public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames) => _inner.FindApiResourcesByNameAsync(apiResourceNames);
    }
}
