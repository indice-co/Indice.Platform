#if NET9_0_OR_GREATER
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides functionality for mapping .NET types to OpenAPI schemas and transforming OpenAPI schemas based on
/// predefined mappings.
/// </summary>
/// <remarks>This class allows developers to define custom mappings between .NET types and OpenAPI schemas,
/// enabling fine-grained control over how types are represented in OpenAPI documentation. It also provides methods to
/// apply these mappings during schema generation and transformation.</remarks>
public static class MappedTypeTransformer
{
    internal static Dictionary<Type, OpenApiSchema> transforms = new ();

    /// <summary>
    /// Maps a specified type to an OpenAPI schema definition.
    /// </summary>
    /// <remarks>This method associates the specified type <typeparamref name="T"/> with the given OpenAPI
    /// schema in order to replace its occurances in the open api document.</remarks>
    /// <typeparam name="T">The type to be mapped to the provided OpenAPI schema.</typeparam>
    /// <param name="schema">The <see cref="OpenApiSchema"/> instance representing the schema definition for the type <typeparamref
    /// name="T"/>.</param>
    public static void MapType<T>(OpenApiSchema schema) {
        transforms[typeof(T)] = schema;
    }


    /// <summary>
    /// Configures the <see cref="OpenApiOptions"/> instance with predefined type mappings and a schema transformer.
    /// </summary>
    /// <remarks>This method maps several common .NET types to their corresponding OpenAPI schema
    /// representations. It also registers a schema transformer to further customize the OpenAPI schema generation
    /// process.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/> instance.</returns>
    public static OpenApiOptions AddMappedTypeTransformer(this OpenApiOptions options) {
        options.MapType<object>(new() { Type = "object" });
        options.MapType<JsonNode>(new() { Type = "object" });
        options.MapType<JsonElement>(new() { Type = "object" });
        options.MapType<Stream>(new() { Type = "string", Format = "binary" });
        options.MapType<IFormFile>(new() { Type = "string", Format = "binary" });
        options.MapType<IFormFileCollection>(new() { Type = "array", Items = new() { Type = "string", Format = "binary" } });
        options.MapType<FilterClause>(new() { Type = "string" });
        options.MapType<GeoPoint>(new() { Type = "string" });
        options.MapType<Base64Id>(new() { Type = "string" });
        options.MapType<GuidOrAlias>(new() { Type = "string" });
        options.MapType<Base64Host>(new() { Type = "string" });
        // Register the type transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        // If transforms contains the schema's type, set the schema type and format from the transform schema
        if (transforms.ContainsKey(context.JsonTypeInfo.Type)) {
            TransformSchema(schema, context.JsonTypeInfo.Type);
        }
        if (schema.Properties is not null) {
            foreach (var jsonProperty in context.JsonTypeInfo.Properties) {
                if (!schema.Properties.TryGetValue(jsonProperty.Name, out var property)) {
                    continue;
                }
                // If transforms contains the property type, set the property schema type and format from the transform schema
                if (transforms.ContainsKey(jsonProperty.PropertyType)) {
                    TransformSchema(property, jsonProperty.PropertyType);
                    continue;
                }
                if (property.Type == "array" && jsonProperty.PropertyType.TryGetAnyElementType(out var elementType) && transforms.ContainsKey(elementType!)) {
                    schema.Items ??= new OpenApiSchema();
                    TransformSchema(property.Items, elementType!);
                    continue;
                }
            }
        }
        if (context.ParameterDescription is not null && 
            transforms.ContainsKey(context.ParameterDescription.Type)) {
            TransformSchema(schema, context.ParameterDescription.Type);
            return Task.CompletedTask;
        }
        if (context.ParameterDescription is not null && schema.Type == "array" && 
            context.ParameterDescription.Type.TryGetAnyElementType(out var parameterElementType) && 
            transforms.ContainsKey(parameterElementType!)) {
            schema.Items ??= new OpenApiSchema();
            TransformSchema(schema.Items, parameterElementType!);
            return Task.CompletedTask;
        }
        if (context.JsonPropertyInfo is not null && transforms.ContainsKey(context.JsonPropertyInfo.PropertyType)) {
            TransformSchema(schema, context.JsonPropertyInfo.PropertyType);
        }
        return Task.CompletedTask;
    }

    private static void TransformSchema(OpenApiSchema schema, Type type) {
        OpenApiSchema transformedSchema = transforms[type];
        schema.Type = transformedSchema.Type;
        schema.Format = transformedSchema.Format;
        schema.Annotations?.Clear();
    }
}
#endif