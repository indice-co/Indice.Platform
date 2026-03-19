#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
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
                FixEmptyDictionarySchemas((OpenApiSchema)property, jsonProperty.PropertyType);
            }
        }
        if (context.ParameterDescription is not null) {
            FixEmptyDictionarySchemas(schema, context.ParameterDescription.Type);
        }
        return Task.CompletedTask;
    }
    private static void FixEmptyDictionarySchemas(OpenApiSchema schema, Type type) {
        var canTransform = schema.Type.HasValue && schema.Type!.Value.HasFlag(JsonSchemaType.Object) && (schema.Properties is null || schema.Properties?.Count == 0) &&
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
                valueSchema.Type = JsonSchemaType.Integer;
                valueSchema.Format = "int32";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(long):
                valueSchema.Type = JsonSchemaType.Integer;
                valueSchema.Format = "int64";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(Guid):
                valueSchema.Type = JsonSchemaType.String;
                valueSchema.Format = "uuid";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(decimal):
                valueSchema.Type = JsonSchemaType.Number;
                valueSchema.Format = "double";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(double):
                valueSchema.Type = JsonSchemaType.Number;
                valueSchema.Format = "double";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(DateTime):
                valueSchema.Type = JsonSchemaType.String;
                valueSchema.Format = "date-time";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(DateTimeOffset):
                valueSchema.Type = JsonSchemaType.String;
                valueSchema.Format = "date-time";
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(string):
                valueSchema.Type = JsonSchemaType.String;
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(bool):
                valueSchema.Type = JsonSchemaType.Boolean;
                if (nullable == true) {
                    valueSchema.Type |= JsonSchemaType.Null;
                }
                break;
        }
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = valueSchema;
    }
}
#endif