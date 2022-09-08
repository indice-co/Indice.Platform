using Microsoft.AspNetCore.Http;

namespace Indice.Features.MultiTenancy.AspNetCore
{
    /// <summary>Provides access to the resolved tenant (cached) for the current operation (request). Similar to the <seealso cref="IHttpContextAccessor"/>.</summary>
    /// <typeparam name="TTenant">The type of tenant.</typeparam>
    public interface ITenantAccessor<TTenant> where TTenant : Tenant
    {
        /// <summary>The current tenant.</summary>
        TTenant Tenant { get; }
    }

    /// <summary>Provides access to the resolved tenant (cached) for the current operation (request) through the <seealso cref="IHttpContextAccessor"/>.</summary>
    /// <typeparam name="TTenant">The type of tenant.</typeparam>
    public class TenantAccessor<TTenant> : ITenantAccessor<TTenant> where TTenant : Tenant
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>Creates a new instance of <see cref="TenantAccessor{TTenant}"/>.</summary>
        /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
        public TenantAccessor(IHttpContextAccessor httpContextAccessor) {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc/>
        public TTenant Tenant => _httpContextAccessor.HttpContext.GetTenant<TTenant>();
    }
}
