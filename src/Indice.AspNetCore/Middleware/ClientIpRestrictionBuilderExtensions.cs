using Indice.AspNetCore.Middleware;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Extension methods related to <see cref="IApplicationBuilder"/></summary>
public static class ClientIpRestrictionBuilderExtensions
{
    /// <summary>Adds client ip restrictions to the <see cref="IApplicationBuilder"/> request execution pipeline. </summary>
    /// <remarks> Must be placed early on in the pipeline in order to intercept calls.</remarks>
    /// <param name="builder"></param>
    /// <returns>The builder</returns>
    public static IApplicationBuilder UseClientIpRestrictions(this IApplicationBuilder builder) {
        return builder.UseMiddleware<ClientIpRestrictionMiddleware>();
    }
}
