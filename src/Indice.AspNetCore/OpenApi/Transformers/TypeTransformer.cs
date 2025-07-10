#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Azure;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

internal static class TypeTransformer
{
    internal static Dictionary<Type, OpenApiSchema> transforms = new Dictionary<Type, OpenApiSchema>();
    internal static Dictionary<string, Type> transformsMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
    public static void MapType<T>(OpenApiSchema schema) {
        transforms[typeof(T)] = schema;
        transformsMap[typeof(T).Name] = typeof(T);
    }

    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        // If transforms contains the schema's type, set the schema type and format from the transform schema
        if (transforms.ContainsKey(context.JsonTypeInfo.Type)) {
            OpenApiSchema transformedSchema = transforms[context.JsonTypeInfo.Type];
            schema.Type = transformedSchema.Type;
            schema.Format = transformedSchema.Format;
            schema.Annotations.Clear();
        }
        if (context.ParameterDescription is not null && transforms.ContainsKey(context.ParameterDescription.Type)) {
            OpenApiSchema transformedSchema = transforms[context.ParameterDescription.Type];
            schema.Type = transformedSchema.Type;
            schema.Format = transformedSchema.Format;
            schema.Annotations.Clear();
        }
        if (schema.Properties is not null) {
            foreach (var property in schema.Properties) {
                if (property.Value.Annotations?.TryGetValue("x-schema-id", out var schemaId) == true && 
                    transformsMap.TryGetValue($"{schemaId}", out var type)) {
                    OpenApiSchema transformedSchema = transforms[type];
                    property.Value.Type = transformedSchema.Type;
                    property.Value.Format = transformedSchema.Format;
                    property.Value.Annotations?.Clear();
                }
            }
        }
        return Task.CompletedTask;
    }
}
#endif