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
    }
}
