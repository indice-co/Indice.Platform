using Indice.AspNetCore.EmbeddedUI;
using Indice.Features.Risk.UI;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods on <see cref="IApplicationBuilder"/>, used to register the <see cref="SpaUIMiddleware{TOptions}"/> middleware.</summary>
public static class RisksUIMiddlewareExtensions
{
    /// <summary>Registers the Risks UI single page application, using the provided options.</summary>
    /// <param name="builder"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseRisksUI(this IApplicationBuilder builder, Action<RisksUIOptions>? options = null) {
        return builder.UseSpaUI("risks-app", typeof(RisksUIMiddlewareExtensions).Assembly, options);
    }
}
