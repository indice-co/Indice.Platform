using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy.Stores
{
    /// <summary>
    /// In memory store for testing
    /// </summary>
    public class InMemoryTenantStore<T> : ITenantStore<T> where T : Tenant
    {
        /// <summary>
        /// Constructs the <see cref="InMemoryTenantStore{T}"/> given a list of available tenants.
        /// </summary>
        /// <param name="tenants"></param>
        public InMemoryTenantStore(IEnumerable<T> tenants) {
            Tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        }

        internal IEnumerable<T> Tenants { get; }

        /// <inheritdoc />
        public Task<int?> GetAccessLevelAsync(string tenantId, string userId) {
            var tenant = Tenants.SingleOrDefault(t => t.Id == tenantId);
            if (tenant is null)
                return Task.FromResult<int?>(null);
            
            if (tenant.Items.ContainsKey(userId)) {
                var accessLevel = (int)tenant.Items[userId];
                Task.FromResult(accessLevel); 
            }
            return Task.FromResult<int?>(null);
        }

        /// <inheritdoc />
        public Task<T> GetTenantAsync(string identifier) {
            var tenant = Tenants.SingleOrDefault(t => t.Identifier == identifier);
            return Task.FromResult(tenant);
        }
    }
}
