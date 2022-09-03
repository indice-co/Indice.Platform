using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy.Stores
{
    /// <summary>In memory store for testing purposes.</summary>
    public class InMemoryTenantStore<TTenant> : ITenantStore<TTenant> where TTenant : Tenant
    {
        /// <summary>Constructs the <see cref="InMemoryTenantStore{T}"/> given a list of available tenants.</summary>
        /// <param name="tenants">The list of tenants.</param>
        public InMemoryTenantStore(IEnumerable<TTenant> tenants) {
            Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        }

        internal IEnumerable<TTenant> Tenants { get; }

        /// <inheritdoc />
        public Task<int?> GetAccessLevelAsync(Guid tenantId, string userId) => Task.FromResult<int?>(2);

        /// <inheritdoc />
        public Task<TTenant> GetTenantAsync(string identifier) {
            var tenant = Tenants.SingleOrDefault(t => t.Identifier == identifier);
            return Task.FromResult(tenant);
        }
    }
}
