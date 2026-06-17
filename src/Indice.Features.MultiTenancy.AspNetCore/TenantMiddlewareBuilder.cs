using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Indice.Features.MultiTenancy.AspNetCore;

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
    private readonly string _relativeTo = "Microsoft.AspNetCore.Routing.EndpointRoutingMiddleware";
    public TenantMiddlewareBuilder(
        IApplicationBuilder inner,
        Action<IApplicationBuilder> injectMiddleware) {
        InnerBuilder = inner;
        _injectMiddleware = injectMiddleware;
    }

    private IApplicationBuilder InnerBuilder { get; }

    public IServiceProvider ApplicationServices {
        get => InnerBuilder.ApplicationServices;
        set => InnerBuilder.ApplicationServices = value;
    }

    public IDictionary<string, object?> Properties => InnerBuilder.Properties;
    public IFeatureCollection ServerFeatures => InnerBuilder.ServerFeatures;
    public RequestDelegate Build() => InnerBuilder.Build();
    public IApplicationBuilder New() => InnerBuilder.New();

    public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) {
        InnerBuilder.Use(middleware);
        if (!_injected) {
            var type = middleware.Target is not null ? GetMiddlewareType(middleware.Target) : null;
            var isMatch = type is not null
                && (type.FullName == _relativeTo || type.Name == _relativeTo);
            if (isMatch) {
                _injected = true;
                _injectMiddleware(InnerBuilder);
            }
        }
        return this;
    }

    /// <summary>
    /// <see cref="UseMiddlewareExtensions.UseMiddleware{T}"/> creates an internal binder object
    /// (e.g. <c>ReflectionMiddlewareBinder</c>) that captures the concrete middleware
    /// <see cref="Type"/> in a private field or property. We read it via reflection so we can
    /// match against <see cref="_relativeTo"/> without taking a hard dependency on ASP.NET Core internals.
    /// </summary>
    private static Type? GetMiddlewareType(object binder) {
        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        return binder.GetType()
            .GetMembers(flags)
            .Select(m => m switch {
                FieldInfo f when f.FieldType == typeof(Type) => f.GetValue(binder) as Type,
                PropertyInfo p when p.PropertyType == typeof(Type) => p.GetValue(binder) as Type,
                _ => null
            })
            .FirstOrDefault(t => t is not null);
    }
}