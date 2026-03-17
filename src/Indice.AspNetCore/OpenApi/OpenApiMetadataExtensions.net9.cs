#if !NET10_0_OR_GREATER
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.AspNetCore.Builder;

/// <summary>Endpoint conventions regarding Open API.</summary>
public static class OpenApiMetadataExtensions
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
    /// <summary>Adds the ApiKey security scheme to the Open API description.</summary>
    /// <param name="builder">Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.</param>
    /// <param name="schemeName"></param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static IEndpointConventionBuilder WithApiKeySecurityRequirement(this IEndpointConventionBuilder builder, string schemeName = "ApiKey") {
        var scheme = new OpenApiSecurityScheme {
            Type = SecuritySchemeType.ApiKey,
            Scheme = schemeName,
            Description = "Enter the api key to get access",
            Name = "X-Api-Key",
            Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme,
                Id = "ApiKey"
            },
            In = ParameterLocation.Header
        };
        builder.WithMetadata(new OpenApiSecurityRequirement() {
            [scheme] = []
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

    /// <summary>
    /// Adds an OpenAPI example to the endpoint metadata for the specified request body type.
    /// </summary>
    /// <remarks>Use this method to enhance OpenAPI documentation by providing concrete example responses for
    /// endpoints. This can improve client generation and API discoverability.</remarks>
    /// <typeparam name="T">The type of the example object to associate with the endpoint response.</typeparam>
    /// <param name="builder">The endpoint convention builder to which the example metadata will be added.</param>
    /// <param name="example">An object representing the example response to include in the OpenAPI documentation. Cannot be null.</param>
    /// <param name="summary">An optional short summary describing the example. If null, no summary is included.</param>
    /// <param name="description">An optional detailed description of the example. If null, no description is included.</param>
    /// <param name="contentType">The media type of the example content. Defaults to "application/json".</param>
    /// <returns>The original endpoint convention builder with the example metadata added.</returns>
    public static IEndpointConventionBuilder WithExampleRequestBody<T>(this IEndpointConventionBuilder builder, T example, string? summary = null, string? description = null, string contentType = MediaTypeNames.Application.Json) where T : class 
        => builder.WithMetadata(new EndpointBodyExampleMetadata(nameof(T), example.ToOpenApiAny(), Summary: summary, Description: description, ContentType: contentType));

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
#endif