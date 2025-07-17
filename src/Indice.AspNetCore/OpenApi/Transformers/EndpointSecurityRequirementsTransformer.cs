#if NET9_0_OR_GREATER
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.Extensions.DependencyInjection;
internal static class EndpointSecurityRequirementsTransformer
{
    public static OpenApiOptions AddEndpointSecurityRequirementsTransformer(this OpenApiOptions options)
    {

        options.AddOperationTransformer((operation, context, cancellationToken) => {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiSecurityRequirement>().Any() &&
                !context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) {
                var securityRequirements = context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiSecurityRequirement>();
                operation.Security = [.. securityRequirements];
            }
            return Task.CompletedTask;
        });
        return options;
    }
}
#endif