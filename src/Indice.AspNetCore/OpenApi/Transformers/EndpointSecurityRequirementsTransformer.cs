#if NET10_0_OR_GREATER
using Indice.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

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
    public static OpenApiOptions AddEndpointSecurityRequirementsTransformer(this OpenApiOptions options) {

        options.AddOperationTransformer((operation, context, cancellationToken) => {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<OpenApiSecurityRequirement>().Any()) {
                var securityRequirements = context.Description.ActionDescriptor
                                                              .EndpointMetadata
                                                              .OfType<OpenApiSecurityRequirement>();
                operation.Security = [];
                foreach (var item in securityRequirements) {
                    var requirement = item.First();
                    operation.Security.Add(new OpenApiSecurityRequirement() {
                        [new(requirement.Key.Reference!.Id!, context.Document)] = requirement.Value ?? []
                    });
                }
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) {
                    operation.Security.Add(new());
                }
            }

            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<AuthorizeAttribute>().Any()) {
                operation.Security = [];
                
                if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) {
                    operation.Security.Add(new());
                    return Task.CompletedTask;
                }
                var securityRequirements = context.Description.ActionDescriptor
                                                             .EndpointMetadata
                                                             .OfType<AuthorizeAttribute>();
                // Read scopes from configuration — document.Components.SecuritySchemes is not yet
                // populated when operation transformers run (document transformers execute after).
                var configuration = context.ApplicationServices.GetRequiredService<IConfiguration>();
                var apiSettings = configuration.GetApiSettings() ?? new ApiSettings();
                var allScopes = GetScopes(apiSettings).Keys.ToList();
                foreach (var item in securityRequirements) {
                    operation.Security.Add(new OpenApiSecurityRequirement() {
                        [new(item.AuthenticationSchemes ?? "oauth2", context.Document)] = allScopes
                    });
                }
            }
            return Task.CompletedTask;
        });
        return options;
    }

    private static Dictionary<string, string> GetScopes(ApiSettings? settings) {
        settings ??= new ApiSettings();
        // Define the OAuth2.0 scheme that's in use (i.e. Implicit Flow).
        var scopes = new Dictionary<string, string> {
            [settings.ResourceName] = $"Access to {settings.FriendlyName}",
        };
        foreach (var scope in settings.Scopes) {
            scopes.Add(scope.Name, scope.Description ?? scope.Name);
        }
        return scopes;
    }
}
#endif