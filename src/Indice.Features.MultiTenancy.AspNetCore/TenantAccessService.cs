using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Indice.Features.MultiTenancy.AspNetCore
{
    /// <summary>Tenant access service.</summary>
    /// <typeparam name="TTenant">The type of tenant.</typeparam>
    public class TenantAccessService<TTenant> where TTenant : Tenant
    {
        private readonly IEnumerable<ITenantResolutionStrategy> _tenantResolutionStrategies;
        private readonly ITenantStore<TTenant> _tenantStore;

        /// <summary>Creates a new instance of <see cref="DefaultTenantAccessService"/>.</summary>
        /// <param name="tenantResolutionStrategies">The list of registered resolution strategies.</param>
        /// <param name="tenantStore">The tenant store.</param>
        public TenantAccessService(IEnumerable<ITenantResolutionStrategy> tenantResolutionStrategies, ITenantStore<TTenant> tenantStore) {
            _tenantResolutionStrategies = tenantResolutionStrategies ?? throw new ArgumentNullException(nameof(tenantResolutionStrategies));
            _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        }

        /// <summary>Gets the current tenant.</summary>
        public async Task<TTenant> GetTenantAsync() {
            string tenantIdentifier = null;
            foreach (var strategy in _tenantResolutionStrategies) {
                tenantIdentifier = await strategy.GetTenantIdentifierAsync();
                if (tenantIdentifier != null)
                    break;
            }
            return await _tenantStore.GetTenantAsync(tenantIdentifier);
        }
    }

    /// <summary>Default tenant access service.</summary>
    public class DefaultTenantAccessService : TenantAccessService<Tenant>
    {
        /// <inheritdoc/>
        public DefaultTenantAccessService(IEnumerable<ITenantResolutionStrategy> tenantResolutionStrategies, ITenantStore<Tenant> tenantStore) : base(tenantResolutionStrategies, tenantStore) { }
    }
}
