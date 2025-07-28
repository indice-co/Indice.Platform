#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This class provides a transformer for handling array schemas in OpenAPI documents.
/// </summary>
public static class ArrayTransformer
{
    /// <summary>
    /// Adds a schema transformer to handle array transformations in OpenAPI options.
    /// </summary>
    /// <remarks>This method registers a schema transformer that processes array schemas in OpenAPI documents.
    /// It modifies the behavior of the OpenAPI generation pipeline to handle arrays in a specific way.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the array transformer registered.</returns>
    public static OpenApiOptions AddArrayTransformer(this OpenApiOptions options) {

        // Register the schema transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }


    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (context.JsonTypeInfo.Properties.Count > 0) {
            transforms.TryAdd(context.JsonTypeInfo.Type, schema);
        }
        if (schema.Properties is not null) {
            foreach (var jsonProperty in context.JsonTypeInfo.Properties) {
                if (!schema.Properties.TryGetValue(jsonProperty.Name, out var property)) {
                    continue;
                }
                FixEmptyArraySchemas(property, jsonProperty.PropertyType);
                if (property.AdditionalProperties is not null && property.AdditionalProperties.Type == "array" &&
                    jsonProperty.PropertyType.GenericTypeArguments?.Length == 2 &&
                    jsonProperty.PropertyType.IsDictionary()) {
                    FixEmptyArraySchemas(property.AdditionalProperties, jsonProperty.PropertyType.GenericTypeArguments[1]);
                }
            }
        }

        if (context.ParameterDescription is not null && schema.Type == "array") {
            FixEmptyArraySchemas(schema, context.ParameterDescription.Type);
        }

        if (context.ParameterDescription is null && context.JsonPropertyInfo is null && schema.Type == "array") {
            FixEmptyArraySchemas(schema, context.JsonTypeInfo.Type);
        }

        return Task.CompletedTask;
    }
    internal static Dictionary<Type, OpenApiSchema> transforms = new Dictionary<Type, OpenApiSchema>();
    private static void FixEmptyArraySchemas(OpenApiSchema schema, Type type) {
        var canTransform = schema.Type == "array" && schema.Items?.Type == null && schema.Items?.Annotations.Any(x => x.Value != null) != true;
        if (!canTransform) {
            return;
        }
        var itemSchema = schema.Items ?? new OpenApiSchema();
        // element type switch.
        var elementType = type.GetAnyElementType();
        bool nullable = false;
        if (elementType is not null) {
            var nullableType = Nullable.GetUnderlyingType(elementType!);
            nullable = nullableType is not null;
            elementType = nullableType ?? elementType;
        }
        switch (elementType) {
            case Type t when t == typeof(int):
                itemSchema.Type = "integer";
                itemSchema.Format = "int32";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(long):
                itemSchema.Type = "integer";
                itemSchema.Format = "int64";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(Guid):
                itemSchema.Type = "string";
                itemSchema.Format = "uuid";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(decimal):
                itemSchema.Type = "number";
                itemSchema.Format = "double";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(double):
                itemSchema.Type = "number";
                itemSchema.Format = "double";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(DateTime):
                itemSchema.Type = "string";
                itemSchema.Format = "date-time";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(DateTimeOffset):
                itemSchema.Type = "string";
                itemSchema.Format = "date-time";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(string):
                itemSchema.Type = "string";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t == typeof(bool):
                itemSchema.Type = "boolean";
                itemSchema.Nullable = nullable;
                break;
            case Type t when t.IsEnum || Nullable.GetUnderlyingType(t)?.IsEnum == true:
                itemSchema.Annotations = new Dictionary<string, object> {
                    ["x-schema-id"] = t.Name
                };
                break;
            default:
                if (elementType is not null && transforms.TryGetValue(elementType!, out var cachedSchema)) {
                    itemSchema = cachedSchema;
                }
                break;
        }
        schema.Items = itemSchema;
    }
}
#endif