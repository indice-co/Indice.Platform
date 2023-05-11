using System.Reflection;
using Indice.AspNetCore.Identity;
using Indice.Features.Identity.UI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/> interface.</summary>
public static class IApplicationBuilderExtensions
{
    /// <summary>Coordinates the page selection process for pages that need to be overridden for specified clients.</summary>
    /// <param name="app">Defines a class that provides the mechanisms to configure an application's request pipeline.</param>
    /// <remarks>Should be used after UseRouting() method is called.</remarks>
    public static void UseIdentityUIThemes(this IApplicationBuilder app) => app.Use(async (httpContext, next) => {
        var currentEndpoint = httpContext.GetEndpoint() as RouteEndpoint;
        if (currentEndpoint is not null) {
            var descriptor = currentEndpoint.Metadata.GetMetadata<CompiledPageActionDescriptor>();
            if (descriptor is not null) {
                var attribute = descriptor.ModelTypeInfo?.GetCustomAttribute<ClientThemeAttribute>();
                if (attribute is not null) {
                    var requestClientId = httpContext.GetClientIdFromReturnUrl();
                    var pageClientId = attribute.ClientId;
                    var clientIdsAreEqual = requestClientId?.Equals(pageClientId, StringComparison.OrdinalIgnoreCase);
                    var shouldSwapEndpoint = !clientIdsAreEqual.HasValue || clientIdsAreEqual.Value == false;
                    if (shouldSwapEndpoint) {
                        var endpointDataSources = httpContext.RequestServices.GetService<IEnumerable<EndpointDataSource>>();
                        var availableEndpoints = endpointDataSources?.SelectMany(x => x.Endpoints);
                        var pageEndpoint = availableEndpoints?
                            .OfType<RouteEndpoint>()
                            .Where(endpoint => endpoint.RoutePattern.RawText == currentEndpoint.RoutePattern.RawText)
                            .OrderByDescending(x => x.Order)
                            .FirstOrDefault();
                        if (pageEndpoint is not null) {
                            httpContext.SetEndpoint(pageEndpoint);
                        }
                    }
                }
            }
        }
        await next(httpContext);
    });
}
