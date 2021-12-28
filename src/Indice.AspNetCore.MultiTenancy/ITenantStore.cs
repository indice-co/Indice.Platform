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
        /// Gets the tenant information form storage using a unique identifier.
        /// </summary>
        /// <param name="identifier">Can be anything from a (sub)domain,name or an alias of some sort</param>
        /// <returns></returns>
        Task<T> GetTenantAsync(string identifier);

        /// <summary>
        /// Gets the access level of a user from the store and the tenant.
        /// </summary>
        /// <param name="tenantId">tenant id</param>
        /// <param name="userId">User id</param>
        /// <returns>The access level.</returns>
        Task<int?> GetAccessLevelAsync(string tenantId, string userId);
    }

    /// <summary>
    /// Extension methods on <see cref="ITenantStore{T}"/>.
    /// </summary>
    public static class TenantStoreExtensions
    {
        /// <summary>
        /// Checks access on behalf of a user against the store and the tenant.
        /// </summary>
        /// <param name="tenantStore"></param>
        /// <param name="tenantId">tenant id</param>
        /// <param name="userId">User id</param>
        /// <param name="requiredLevel">The minimum access level requirement</param>
        /// <returns></returns>
        public static async Task<bool> CheckAccessAsync<T>(this ITenantStore<T> tenantStore, string tenantId, string userId, int? requiredLevel) where T : Tenant {
            var memberLevel = await tenantStore.GetAccessLevelAsync(tenantId, userId);
            if (!requiredLevel.HasValue) { 
                return memberLevel.HasValue;
            }
            return memberLevel >= requiredLevel;
        }
    }
}
