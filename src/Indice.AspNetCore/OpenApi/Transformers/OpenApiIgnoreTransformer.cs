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

        // find all properties of the type that are decorated with the OpenApiIgnoreAttribute 
        var propertiesToIgnoreFromOpenApi = context.JsonTypeInfo.Type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(x => x.IsDefined(typeof(OpenApiIgnoreAttribute), inherit: true))
            .Select(x => schema.Properties!.Keys.FirstOrDefault(k => k.Equals(x.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(x => x is not null)
            .ToList();

        foreach (var property in propertiesToIgnoreFromOpenApi) {
            schema.Properties?.Remove(property!);
            schema.Required?.Remove(property!);
        }

        return Task.CompletedTask;
    }
}
#endif