#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This class provides a transformer for handling dictionary schemas in OpenAPI documents.
/// </summary>
public static class DictionaryTransformer
{
    /// <summary>
    /// Adds a schema transformer to handle dictionary transformations in OpenAPI options.
    /// </summary>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the array transformer registered.</returns>
    public static OpenApiOptions AddDictionaryTransformer(this OpenApiOptions options) {

        // Register the schema transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }


    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (schema.Properties is not null) {
            foreach (var jsonProperty in context.JsonTypeInfo.Properties) {
                if (!schema.Properties.TryGetValue(jsonProperty.Name, out var property)) {
                    continue;
                }
                FixEmptyDictionarySchemas(property, jsonProperty.PropertyType);
            }
        }
        if (context.ParameterDescription is not null) {
            FixEmptyDictionarySchemas(schema, context.ParameterDescription.Type);
        }
        return Task.CompletedTask;
    }
    private static void FixEmptyDictionarySchemas(OpenApiSchema schema, Type type) {
        var canTransform = schema.Type == "object" && schema.Properties.Count == 0 &&
                           schema.AdditionalPropertiesAllowed == true &&
                           schema.AdditionalProperties is null &&
                           type.IsDictionary() && type.GenericTypeArguments.Length == 2 && type.GenericTypeArguments[1].IsPrimitive();

        if (!canTransform) { 
            return;
        }
        var valueType = type.GenericTypeArguments[1];
        var valueSchema = new OpenApiSchema();
        var nullableType = Nullable.GetUnderlyingType(valueType!);
        bool nullable = nullableType != null;
        valueType = nullableType ?? valueType;
        // type switch.
        switch (valueType) {
            case Type t when t == typeof(int):
                valueSchema.Type = "integer";
                valueSchema.Format = "int32";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(long):
                valueSchema.Type = "integer";
                valueSchema.Format = "int64";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(Guid):
                valueSchema.Type = "string";
                valueSchema.Format = "uuid";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(decimal):
                valueSchema.Type = "number";
                valueSchema.Format = "double";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(double):
                valueSchema.Type = "number";
                valueSchema.Format = "double";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(DateTime):
                valueSchema.Type = "string";
                valueSchema.Format = "date-time";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(DateTimeOffset):
                valueSchema.Type = "string";
                valueSchema.Format = "date-time";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(string):
                valueSchema.Type = "string";
                valueSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(bool):
                valueSchema.Type = "boolean";
                valueSchema.Nullable = nullable;
                break;
            default:
                break;
        }
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = valueSchema;
    }
}
#endif