#if NET10_0_OR_GREATER
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds a transformer to the OpenAPI options that applies security requirements to operations based on endpoint
/// metadata.
/// </summary>
/// <remarks>This method ensures that operations in the OpenAPI specification include security requirements
/// defined in the endpoint metadata, unless the endpoint explicitly allows anonymous access.</remarks>
public static class EndpointSecurityRequirementsTransformer
{
    /// <summary>
    /// Adds a transformer to the OpenAPI options that applies security requirements to operations based on endpoint
    /// metadata.
    /// </summary>
    /// <remarks>This method ensures that operations associated with endpoints containing <see
    /// cref="OpenApiSecurityRequirement"/> metadata, but not marked with <see cref="IAllowAnonymous"/>, will have their
    /// security requirements applied in the OpenAPI documentation.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> to which the transformer will be added.</param>
    /// <returns>The modified <see cref="OpenApiOptions"/> instance.</returns>
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