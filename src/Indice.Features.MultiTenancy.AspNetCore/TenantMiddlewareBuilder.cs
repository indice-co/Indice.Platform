using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Indice.Features.Multitenancy.AspNetCore;

/// <summary>
/// An <see cref="IApplicationBuilder"/> decorator that intercepts each <c>Use()</c> call
/// and injects a chosen middleware 
/// The middleware to inject is supplied from outside,
/// keeping this class agnostic of any specific middleware or tenant type.
/// </summary>
internal class TenantMiddlewareBuilder : IApplicationBuilder
{
    private readonly Action<IApplicationBuilder> _injectMiddleware;
    private bool _injected;

    public TenantMiddlewareBuilder(IApplicationBuilder inner, Action<IApplicationBuilder> injectMiddleware) {
        InnerBuilder = inner;
        _injectMiddleware = injectMiddleware;
    }

    private IApplicationBuilder InnerBuilder { get; }

    public IServiceProvider ApplicationServices {
        get => InnerBuilder.ApplicationServices;
        set => InnerBuilder.ApplicationServices = value;
    }

    public IDictionary<string, object> Properties => InnerBuilder.Properties;
    public IFeatureCollection ServerFeatures => InnerBuilder.ServerFeatures;
    public RequestDelegate Build() => InnerBuilder.Build();
    public IApplicationBuilder New() => InnerBuilder.New();

    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) {
        if (!_injected) {
            _injected = true;
            _injectMiddleware(InnerBuilder);
        }
        return InnerBuilder.Use(middleware);
    }
}