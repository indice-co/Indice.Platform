#if NET9_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

internal static class MappedTypeTransformer
{
    internal static Dictionary<Type, OpenApiSchema> transforms = new Dictionary<Type, OpenApiSchema>();
    internal static Dictionary<string, Type> transformsMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    public static void MapType<T>(OpenApiSchema schema) {
        transforms[typeof(T)] = schema;
        transformsMap[typeof(T).Name] = typeof(T);
    }

    public static OpenApiOptions AddMappedTypeTransformer(this OpenApiOptions options) {
        options.MapType<object>(new() { Type = "object" });
        options.MapType<JsonNode>(new() { Type = "object" });
        options.MapType<JsonElement>(new() { Type = "object" });
        options.MapType<Stream>(new() { Type = "string", Format = "binary" });
        options.MapType<IFormFile>(new() { Type = "string", Format = "binary" });
        options.MapType<IFormFileCollection>(new() { Type = "array", Items = new () { Type = "string", Format = "binary" } });
        options.MapType<FilterClause>(new() { Type = "string" });
        options.MapType<GeoPoint>(new() { Type = "string" });
        options.MapType<Base64Id>(new() { Type = "string" });
        options.MapType<GuidOrAlias>(new() { Type = "string" });
        options.MapType<Base64Host>(new() { Type = "string" });
        // Register the type transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        // If transforms contains the schema's type, set the schema type and format from the transform schema
        if (transforms.ContainsKey(context.JsonTypeInfo.Type)) {
            TransformSchema(schema, context.JsonTypeInfo.Type);
        }
        if (schema.Properties is not null) {
            foreach (var property in schema.Properties) {
                if (property.Value.Annotations?.TryGetValue("x-schema-id", out var schemaId) == true &&
                    transformsMap.TryGetValue($"{schemaId}", out var type)) {
                    TransformSchema(property.Value, type);
                }
            }
        }
        if (context.ParameterDescription is not null && transforms.ContainsKey(context.ParameterDescription.Type)) {
            TransformSchema(schema, context.ParameterDescription.Type);
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