using Indice.Features.Multitenancy.AspNetCore;
using Indice.Features.Multitenancy.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Multitenancy.AspNetCore;

/// <summary>Extensions on <see cref="IServiceCollection"/> for ordering tenant middleware.</summary>
internal static class TenantMiddlewareExtensions
{
    /// <summary>
    /// Registers a <see cref="IStartupFilter"/> that injects <see cref="TenantMiddleware{TTenant}"/>
    /// </summary>
    internal static IServiceCollection AddTenantMiddlewareStartupFilter<TTenant>(this IServiceCollection services) where TTenant : Tenant {
        return services.AddTransient<IStartupFilter>(_ =>
            new TenantMiddlewareStartupFilter(app => app.UseMiddleware<TenantMiddleware<TTenant>>()));
    }
}