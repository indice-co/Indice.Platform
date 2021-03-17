using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy.Stores
{
    /// <summary>
    /// In memory store for testing
    /// </summary>
    public class InMemoryTenantStore<T> : ITenantStore<T> where T : Tenant
    {
        /// <summary>
        /// Constructs the <see cref="InMemoryTenantStore{T}"/> given a list of available tenatnts.
        /// </summary>
        /// <param name="tenants"></param>
        public InMemoryTenantStore(IEnumerable<T> tenants) {
            Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        }

        internal IEnumerable<T> Tenants { get; }

        /// <inheritdoc />
        public async Task<T> GetTenantAsync(string identifier) {
            var tenant = Tenants.SingleOrDefault(t => t.Identifier == identifier);
            return await Task.FromResult(tenant);
        }
    }
}
