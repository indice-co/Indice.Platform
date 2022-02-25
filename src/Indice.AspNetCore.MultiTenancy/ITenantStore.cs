using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Indice.AspNetCore.MultiTenancy
{
    /* attribution: https://michael-mckenna.com/multi-tenant-asp-dot-net-core-application-tenant-resolution */
    /// <summary>
    /// If you want to store things like connection strings against a tenant it will need to be somewhere secure and probably best to use the
    /// Options configuration per tenant pattern and load those strings from somewhere secure like Azure Key Vault.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITenantStore<T> where T : Tenant
    {
        /// <summary>
        /// Gets the tenant information form storage using a unique identifier .
        /// </summary>
        /// <param name="identifier">Can be anything from a (sub)domain,name or an alias of some sort</param>
        /// <returns></returns>
        Task<T> GetTenantAsync(string identifier);

        /// <summary>
        /// Checks access on behalf of a user against the store and the tenant.
        /// </summary>
        /// <param name="tenantId">tenant id</param>
        /// <param name="userId">User id</param>
        /// <returns>The user access level</returns>
        Task<int?> GetAccessLevelAsync(Guid tenantId, string userId);
    }

    /// <summary>
    /// Extension methods on the <see cref="ITenantStore{T}"/>
    /// </summary>
    public static class TenantStoreExtensions
    {
        /// <summary>
        /// Checks access on behalf of a user against the store and the tenant.
        /// </summary>
        /// <param name="tenantStore">The store implementation</param>
        /// <param name="tenantId">tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="level">The minimum access level requirement</param>
        /// <returns>True on success</returns>
        public static async Task<bool> CheckAccessAsync<T>(this ITenantStore<T> tenantStore, Guid tenantId, string userId, int? level) where T : Tenant {
            var accessLevel = await tenantStore.GetAccessLevelAsync(tenantId, userId);
            if (accessLevel.HasValue) {
                return accessLevel >= level.GetValueOrDefault();
            }
            return false;
        }
    }
}
