using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Routing;

internal static class IEndpointRouteBuilderExtensions
{
    public static TOptions GetRequiredOptions<TOptions>(this IEndpointRouteBuilder endpointRouteBuilder) where TOptions : class 
        => endpointRouteBuilder.ServiceProvider.GetRequiredService<IOptions<TOptions>>().Value;
}
