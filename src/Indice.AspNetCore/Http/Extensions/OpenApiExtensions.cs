using System.Net.Mime;
using Indice.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Endpoint conventions regarding Open API.</summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Adds an OpenAPI security requirement to the endpoint, specifying the security scheme and required scopes.
    /// </summary>
    /// <remarks>This method is typically used to define security requirements for endpoints in an OpenAPI
    /// specification. The specified security scheme and scopes will be included in the generated OpenAPI
    /// documentation.</remarks>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to which the security requirement is applied.</param>
    /// <param name="securitySchemeId">The identifier of the security scheme to use. Defaults to <see langword="oauth2"/>.</param>
    /// <param name="requiredScopes">The scopes required for the security scheme. If no scopes are specified, the security requirement will be added
    /// without any specific scopes.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> instance with the applied OpenAPI security requirement.</returns>
    public static IEndpointConventionBuilder WithOpenApiSecurityRequirement(this IEndpointConventionBuilder builder, string securitySchemeId = "oauth2", params string[] requiredScopes) {
        var scheme = new OpenApiSecurityScheme() {
            Type = SecuritySchemeType.Http,
            Name = securitySchemeId,
            Scheme = securitySchemeId,
            Reference = new() {
                Type = ReferenceType.SecurityScheme,
                Id = securitySchemeId
            }
        };
        builder.WithMetadata(new OpenApiSecurityRequirement() {
            [scheme] = requiredScopes.ToList() ?? []
        });
        return builder;
    }

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
                    [scheme] = requiredScopes.ToList() ?? []
                }
            },
        });
    }

    /// <summary>Adds the ApiKey security scheme to the Open API description.</summary>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static IEndpointConventionBuilder AddApiKeySecurityRequirement(this IEndpointConventionBuilder builder) {
        var scheme = new OpenApiSecurityScheme {
            Type = SecuritySchemeType.ApiKey,
            Scheme = "ApiKeyScheme",
            Description = "Enter the api key to get access",
            Name = "X-Api-Key",
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "ApiKey"
            },
            In = ParameterLocation.Header
        };

        return builder.WithOpenApi(operation => new(operation) {
            Security = {
                new() {
                    [scheme] = []
                }
            }
        });
    }

    /// <summary>Adds enum support if needed to a query parameter. Experimental</summary>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="paramName">The parameter name to fix</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static IEndpointConventionBuilder WithOpenApiEnum<TEnum>(this IEndpointConventionBuilder builder, string paramName) {
        return builder.WithOpenApi(operation => {
            var op = new OpenApiOperation(operation);
            var enumType = typeof(TEnum);
            var isNullable = (enumType.IsValueType && Nullable.GetUnderlyingType(enumType) != null) || true;
            var paramSchemaType = enumType.IsFlagsEnum() ? "array" : "string";
            var param = op.Parameters.Where(x => paramName.Equals(x.Name, StringComparison.OrdinalIgnoreCase)).First();

            param.Schema = new OpenApiSchema() {
                Type = "array",
                Format = null,
                Nullable = isNullable,
                Enum = null,
                Items = new OpenApiSchema() {
                    Type = "string",
                    Enum = Enum.GetNames(enumType).Select(name => (IOpenApiAny)new OpenApiString(name)).ToList()
                }
            };
            return op;
        });
    }
    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <see cref="ProblemDetails"/> type
    /// to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteGroupBuilder"/>.</param>
    /// <param name="statusCode">The response status code.</param>
    /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteGroupBuilder ProducesProblem(this RouteGroupBuilder builder, int statusCode, string? contentType = null) =>
        builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(ProblemDetails), [contentType ?? MediaTypeNames.Application.ProblemJson]));

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <see cref="HttpValidationProblemDetails"/> type
    /// to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteGroupBuilder"/>.</param>
    /// <param name="statusCode">The response status code. Defaults to <see cref="StatusCodes.Status400BadRequest"/>.</param>
    /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteGroupBuilder ProducesValidationProblem(this RouteGroupBuilder builder, int statusCode = 400, string? contentType = null)
        => builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(HttpValidationProblemDetails), [contentType ?? MediaTypeNames.Application.ProblemJson]));
}