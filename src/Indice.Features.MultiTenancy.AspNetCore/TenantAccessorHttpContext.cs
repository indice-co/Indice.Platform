using Indice.Features.Multitenancy.Core;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Multitenancy.AspNetCore;

/// <summary>Provides access to the resolved tenant (cached) for the current operation (request) through the <seealso cref="IHttpContextAccessor"/>.</summary>
/// <typeparam name="TTenant">The type of tenant.</typeparam>
public class TenantAccessorHttpContext<TTenant> : ITenantAccessor<TTenant> where TTenant : Tenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>Creates a new instance of <see cref="TenantAccessorHttpContext{TTenant}"/>.</summary>
    /// <param name="httpContextAccessor">Provides access to the current <see cref="HttpContext"/>.</param>
    public TenantAccessorHttpContext(IHttpContextAccessor httpContextAccessor) {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public TTenant Tenant => _httpContextAccessor.HttpContext.GetTenant<TTenant>();
}
