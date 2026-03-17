#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
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
        if (schema.Properties is not null) {
            foreach (var jsonProperty in context.JsonTypeInfo.Properties) {
                if (!schema.Properties.TryGetValue(jsonProperty.Name, out var property)) {
                    continue;
                }
                FixEmptyArraySchemas((OpenApiSchema)property, jsonProperty.PropertyType);
                if (property.AdditionalProperties is not null && property.AdditionalProperties.Type!.Value.HasFlag(JsonSchemaType.Array) &&
                    jsonProperty.PropertyType.GenericTypeArguments?.Length == 2 &&
                    jsonProperty.PropertyType.IsDictionary()) {
                    FixEmptyArraySchemas((OpenApiSchema)property.AdditionalProperties, jsonProperty.PropertyType.GenericTypeArguments[1]);
                }
            }
        }

        if (context.ParameterDescription is not null && schema.Type.HasValue && schema.Type!.Value.HasFlag(JsonSchemaType.Array)) {
            FixEmptyArraySchemas(schema, context.ParameterDescription.Type);
        }

        if (context.ParameterDescription is null && context.JsonPropertyInfo is null && schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Array)) {
            FixEmptyArraySchemas(schema, context.JsonTypeInfo.Type);
        }

        return Task.CompletedTask;
    }

    private static void FixEmptyArraySchemas(OpenApiSchema schema, Type type) {
        var canTransform = schema.Type!.Value.HasFlag(JsonSchemaType.Array) && schema.Items?.Type == null;
        if (!canTransform) {
            return;
        }
        OpenApiSchema itemSchema = (schema.Items as OpenApiSchema) ?? new OpenApiSchema();
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
                itemSchema.Type = JsonSchemaType.Integer;
                itemSchema.Format = "int32";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(long):
                itemSchema.Type = JsonSchemaType.Integer;
                itemSchema.Format = "int64";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(Guid):
                itemSchema.Type = JsonSchemaType.String;
                itemSchema.Format = "uuid";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(decimal):
                itemSchema.Type = JsonSchemaType.Number;
                itemSchema.Format = "double";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(double):
                itemSchema.Type = JsonSchemaType.Number;
                itemSchema.Format = "double";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(DateTime):
                itemSchema.Type = JsonSchemaType.String;
                itemSchema.Format = "date-time";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(DateTimeOffset):
                itemSchema.Type = JsonSchemaType.String;
                itemSchema.Format = "date-time";
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(string):
                itemSchema.Type = JsonSchemaType.String;
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
            case Type t when t == typeof(bool):
                itemSchema.Type = JsonSchemaType.Boolean;
                if (nullable == true) {
                    itemSchema.Type |= JsonSchemaType.Null;
                }
                break;
        }
        schema.Items = itemSchema;
    }
}
#endif