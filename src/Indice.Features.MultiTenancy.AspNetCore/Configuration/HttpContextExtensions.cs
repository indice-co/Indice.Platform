using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.Core;

namespace Microsoft.AspNetCore.Http;

/// <summary>Extensions to <see cref="HttpContext"/> to make multi-tenancy easier to use.</summary>
public static class HttpContextExtensions
{
    /// <summary>Returns the current tenant.</summary>
    /// <typeparam name="TTenant">The type of the tenant.</typeparam>
    /// <param name="context">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static TTenant? GetTenant<TTenant>(this HttpContext context) where TTenant : Tenant {
        if (!context.Items.ContainsKey(Constants.HttpContextTenantKey)) {
            return default;
        }
        return context.Items[Constants.HttpContextTenantKey] as TTenant;
    }

    /// <summary>Returns the current tenant.</summary>
    /// <param name="context">Encapsulates all HTTP-specific information about an individual HTTP request.</param>
    public static Tenant? GetTenant(this HttpContext context) => context.GetTenant<Tenant>();
}
