using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.MultiTenancy
{
    /// <summary>
    /// Provides access to the rsolved tenant (cached) for the current operation (request). Similar to the <seealso cref="IHttpContextAccessor"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITenantAccessor<T> where T : Tenant
    {
        /// <summary>
        /// The current tenant
        /// </summary>
        T Tenant { get; }
    }

    /// <summary>
    /// Provides access to the rsolved tenant (cached) for the current operation (request) through the <seealso cref="IHttpContextAccessor"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TenantAccessor<T> : ITenantAccessor<T> where T : Tenant
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// constructs the service
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public TenantAccessor(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public T Tenant => _httpContextAccessor.HttpContext.GetTenant<T>();
    }
}
