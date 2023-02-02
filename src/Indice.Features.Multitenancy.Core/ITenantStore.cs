/* 
 * Attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution 
 */

using System;
using System.Threading.Tasks;

namespace Indice.Features.Multitenancy.Core
{
    /// <summary>
    /// If you want to store things like connection strings against a tenant it will need to be somewhere secure and probably 
    /// best to use the options configuration per tenant pattern and load those strings from somewhere secure like Azure Key Vault.</summary>
    /// <typeparam name="TTenant">The type of the tenant.</typeparam>
    public interface ITenantStore<TTenant> where TTenant : Tenant
    {
        /// <summary>Gets the tenant information from storage using a unique identifier.</summary>
        /// <param name="identifier">Can be anything from a (sub)domain, name or an alias of some sort.</param>
        Task<TTenant> GetTenantAsync(string identifier);
        /// <summary>Checks access on behalf of a user against the store and the tenant.</summary>
        /// <param name="tenantId">The id of the tenant.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>The user access level.</returns>
        Task<int?> GetAccessLevelAsync(Guid tenantId, string userId);
        /// <summary>Logs activity for the given user/member in the given tenant.</summary>
        /// <param name="tenantId">The id of the tenant.</param>
        /// <param name="userId">The id of the user.</param>
        /// <returns>This will essentialy update the LastAccessDate in the TenantMember store.</returns>
        Task LogActivityAsync(Guid tenantId, string userId);
    }

    /// <summary>Extension methods on the <see cref="ITenantStore{T}"/>.</summary>
    public static class TenantStoreExtensions
    {
        /// <summary>Checks access on behalf of a user against the store and the tenant.</summary>
        /// <param name="tenantStore">The store implementation</param>
        /// <param name="tenantId">The id of the tenant.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="level">The minimum access level requirement.</param>
        public static async Task<bool> CheckAccessAsync<T>(this ITenantStore<T> tenantStore, Guid tenantId, string userId, int? level) where T : Tenant {
            var accessLevel = await tenantStore.GetAccessLevelAsync(tenantId, userId);
            if (accessLevel.HasValue) {
                return accessLevel >= level.GetValueOrDefault();
            }
            return false;
        }
    }
}
