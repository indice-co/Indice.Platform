using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy
{
    /// <summary>
    /// Tenant access service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TenantAccessService
        <T> where T : Tenant
    {
        private readonly IEnumerable<ITenantResolutionStrategy> _tenantResolutionStrategies;
        private readonly ITenantStore<T> _tenantStore;

        /// <summary>
        /// constructs the Tenantaccess service.
        /// </summary>
        /// <param name="tenantResolutionStrategies"></param>
        /// <param name="tenantStore"></param>
        public TenantAccessService(IEnumerable<ITenantResolutionStrategy> tenantResolutionStrategies, ITenantStore<T> tenantStore) {
            _tenantResolutionStrategies = tenantResolutionStrategies ?? throw new ArgumentNullException(nameof(tenantResolutionStrategies));
            _tenantStore = tenantStore ?? throw new ArgumentNullException(nameof(tenantStore));
        }

        /// <summary>
        /// Get the current tenant
        /// </summary>
        /// <returns></returns>
        public async Task<T> GetTenantAsync() {
            string tenantIdentifier = null;
            foreach (var strategy in _tenantResolutionStrategies) {
                tenantIdentifier = await strategy.GetTenantIdentifierAsync();
                if (tenantIdentifier != null)
                    break;
            }
            return await _tenantStore.GetTenantAsync(tenantIdentifier);
        }
    }

    /// <summary>
    /// Default Tenant access service
    /// </summary>
    public class TenantAccessService : TenantAccessService<Tenant> {

        /// <inheritdoc/>
        public TenantAccessService(IEnumerable<ITenantResolutionStrategy> tenantResolutionStrategies, ITenantStore<Tenant> tenantStore) 
            : base(tenantResolutionStrategies, tenantStore) { 
        }
    }
}
