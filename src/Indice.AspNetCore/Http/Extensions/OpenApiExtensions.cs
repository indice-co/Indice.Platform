#if NET7_0_OR_GREATER
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Endpoint conventions regarding Open API.</summary>
public static class OpenApiExtensions
{
    /// <summary>Adds the JWT security scheme to the Open API description.</summary>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="securityScheme">The security scheme to use.</param>
    /// <param name="requiredScopes">The array of required scopes.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static IEndpointConventionBuilder AddOpenApiSecurityRequirement(this IEndpointConventionBuilder builder, string securityScheme = "oauth2", params string[] requiredScopes) {
        var scheme = new OpenApiSecurityScheme() {
            Type = SecuritySchemeType.Http,
            Name = securityScheme,
            Scheme = securityScheme,
            Reference = new() {
                Type = ReferenceType.SecurityScheme,
                Id = securityScheme
            }
        };
        return builder.WithOpenApi(operation => new(operation) {
            Security = {
                new() {
                    [scheme] = requiredScopes.ToList() ?? new List<string>()
                }
            }
        });
    }
}
#endif