using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Indice.Features.Multitenancy.AspNetCore;

/// <summary>
/// An <see cref="IStartupFilter"/> that wraps the application builder with a
/// <see cref="TenantMiddlewareBuilder"/> so that a chosen middleware is injected
/// Both the middleware delegate and the target type name are supplied from outside,
/// keeping this class agnostic of both the tenant type and the concrete middleware type.
/// </summary>
internal class TenantMiddlewareStartupFilter : IStartupFilter
{
    private readonly Action<IApplicationBuilder> _injectMiddleware;

    public TenantMiddlewareStartupFilter(Action<IApplicationBuilder> injectMiddleware) {
        _injectMiddleware = injectMiddleware;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
        return builder => {
            var wrappedBuilder = new TenantMiddlewareBuilder(builder, _injectMiddleware);
            next(wrappedBuilder);
        };
    }
}