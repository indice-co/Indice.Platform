#if NET7_0_OR_GREATER
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
            },
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
    public static RouteGroupBuilder ProducesProblem(this RouteGroupBuilder builder, int statusCode, string contentType = null) => 
        builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(ProblemDetails), [ contentType ?? "application/problem+json" ]));

    /// <summary>
    /// Adds an <see cref="IProducesResponseTypeMetadata"/> with a <see cref="HttpValidationProblemDetails"/> type
    /// to <see cref="EndpointBuilder.Metadata"/> for all endpoints produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="RouteGroupBuilder"/>.</param>
    /// <param name="statusCode">The response status code. Defaults to <see cref="StatusCodes.Status400BadRequest"/>.</param>
    /// <param name="contentType">The response content type. Defaults to "application/problem+json".</param>
    /// <returns>A <see cref="RouteGroupBuilder"/> that can be used to further customize the endpoint.</returns>
    public static RouteGroupBuilder ProducesValidationProblem(this RouteGroupBuilder builder, int statusCode = 400, string contentType = null) 
        => builder.WithMetadata(new ProducesResponseTypeMetadata(statusCode, typeof(HttpValidationProblemDetails), [ contentType ?? "application/problem+json" ]));
}
#endif