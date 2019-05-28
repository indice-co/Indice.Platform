using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Indice.AspNetCore.Identity.Scopes
{

    /// <summary>
    /// Dynamic scopes decorator for IResourceStore
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="IdentityServer4.Stores.IResourceStore" />
    public class DynamicScopeResourceStore<T> : IResourceStore
        where T : IResourceStore
    {
        private readonly IResourceStore _inner;


        /// <summary>
        /// Initializes a new instance of the <see cref="DynamicScopeResourceStore{T}" /> class.
        /// </summary>
        public DynamicScopeResourceStore(T inner) {
            _inner = inner;
        }

        /// <summary>
        /// Gets all resources.
        /// </summary>
        /// <returns></returns>
        public Task<Resources> GetAllResourcesAsync() => _inner.GetAllResourcesAsync();

        /// <summary>
        /// Finds the API resource by name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Task<ApiResource> FindApiResourceAsync(string name) => _inner.FindApiResourceAsync(name);

        /// <summary>
        /// Finds the identity resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">names</exception>
        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> names) => _inner.FindIdentityResourcesByScopeAsync(names);

        /// <summary>
        /// Finds the API resources by scope.
        /// </summary>
        /// <param name="names">The names.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">names</exception>
        public async Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> names) {
            if (names == null) throw new ArgumentNullException(nameof(names));

            var resources = await GetAllResourcesAsync();
            var apis = new List<ApiResource>();
            foreach (var api in resources.ApiResources) {
                var matches = api.Scopes.FindMatches(names);
                if (!matches.Any())
                    continue;
                var apiClone = new ApiResource {
                    Enabled = api.Enabled,
                    Name = api.Name,
                    DisplayName = api.DisplayName,
                    ApiSecrets = api.ApiSecrets,
                    Scopes = new HashSet<Scope>(matches.ToArray()),
                    UserClaims = api.UserClaims
                };
                apis.Add(apiClone);
            }

            return apis;
        }
    }
}
