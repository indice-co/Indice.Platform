#if NET10_0_OR_GREATER
using System.Reflection;
using Indice.AspNetCore.OpenApi.Attributes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides functionality to remove properties decorated with <see cref="OpenApiIgnoreAttribute"/> from OpenAPI schema generation.
/// </summary>
public static class OpenApiIgnoreTransformer
{
    /// <summary>
    /// Adds a schema transformer to the specified OpenAPI options that removes properties
    /// marked with <see cref="OpenApiIgnoreAttribute"/> from the generated schema.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The <see cref="OpenApiOptions"/> instance with the ignore transformer added.</returns>
    public static OpenApiOptions AddOpenApiIgnoreTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    private static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (schema.Properties?.Count == 0) {
            return Task.CompletedTask;
        }

        // Find all JSON properties whose underlying member is decorated with OpenApiIgnoreAttribute.
        // Use the JSON property name so renamed properties (for example via JsonPropertyName) are removed correctly.
        var propertiesToIgnoreFromOpenApi = context.JsonTypeInfo.Properties
            .Where(x => x.AttributeProvider?.IsDefined(typeof(OpenApiIgnoreAttribute), inherit: true) == true)
            .Select(x => x.Name)
            .ToList();

        foreach (var property in propertiesToIgnoreFromOpenApi) {
            schema.Properties?.Remove(property);
            schema.Required?.Remove(property);
        }

        return Task.CompletedTask;
    }
}
#endif