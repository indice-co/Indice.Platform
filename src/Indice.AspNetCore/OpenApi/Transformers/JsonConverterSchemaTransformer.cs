#if NET10_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Schema;
using Indice.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This class provides a transformer for handling array schemas in OpenAPI documents.
/// </summary>
public static class JsonConverterSchemaTransformer
{


    /// <summary>
    /// Adds a schema transformer to handle array transformations in OpenAPI options.
    /// </summary>
    /// <remarks>This method registers a schema transformer that processes array schemas in OpenAPI documents.
    /// It modifies the behavior of the OpenAPI generation pipeline to handle arrays in a specific way.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the array transformer registered.</returns>
    public static OpenApiOptions AddJsonConverterTransformer(this OpenApiOptions options) {

        // Register the schema transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (schema.Type is null && schema.Enum is null) {
            switch (context.JsonTypeInfo?.Converter) {
                case not null when context.JsonTypeInfo.Converter is TypeConverterJsonAdapterFactory:
                    schema.Type = JsonSchemaType.String;
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is JsonStringDecimalConverter:
                case not null when context.JsonTypeInfo.Converter is JsonStringDoubleConverter:
                case not null when context.JsonTypeInfo.Converter is JsonStringInt32Converter:
                case not null when context.JsonTypeInfo.Converter is JsonStringBooleanConverter:
                case not null when context.JsonTypeInfo.Converter is JsonAnyStringConverter:
                case not null:
                    TransformSchemaPrimitive(schema, context.JsonTypeInfo.Options, context.JsonTypeInfo.Type);
                    if (context.JsonPropertyInfo?.IsGetNullable == false) {
                        schema.Type &= ~JsonSchemaType.Null;
                    }
                    break;
                default:
                    break;
            }
        } else if (schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Array) && schema.Items is null && context.JsonTypeInfo is not null) {
            var schemaItems = new OpenApiSchema();
            schema.Items = schemaItems;
            TransformSchemaPrimitive(schemaItems, context.JsonTypeInfo.Options, context.JsonTypeInfo.ElementType!);
            // for collections never allow nulls for item values.
            // It is difficult to know for sure especially for strings (nullable reference types).
            // This happens because the default schema generation of system.text.json for strings,
            // always infers a string as a nullable type.
            schemaItems.Type &= ~JsonSchemaType.Null; 
            if (context.JsonPropertyInfo?.IsGetNullable == true) {
                schema.Type |= JsonSchemaType.Null;
            }
        } else if (context.JsonTypeInfo is not null && IsDictionatySchema(schema, context.JsonTypeInfo.Type)) {
            FixEmptyDictionaryItemSchema(schema, context.JsonTypeInfo.Options, context.JsonTypeInfo.Type);
            if (context.JsonPropertyInfo?.IsGetNullable == true) {
                schema.Type |= JsonSchemaType.Null;
            }
        }
        return Task.CompletedTask;
    }

    private static JsonSerializerOptions? _WebDefalts;
    private static void TransformSchemaPrimitive(OpenApiSchema schema, JsonSerializerOptions runtimeJsonOptions, Type type) {
        _WebDefalts ??= StripCustomConverters(runtimeJsonOptions);
        var defaultSchema = _WebDefalts.GetJsonSchemaAsNode(type);
        // Guard: if the schema is not a JsonObject (e.g. bare 'true'/'false' schema), bail out.
        if (defaultSchema is not System.Text.Json.Nodes.JsonObject) {
            return;
        }
        var isArrayOfTypes = defaultSchema["type"]?.GetValueKind() == JsonValueKind.Array;
        var defaultTypes = isArrayOfTypes ? string.Join(',', defaultSchema["type"]!.AsArray().Select(x => x!.ToString())) : defaultSchema["type"]?.ToString();
        schema.Pattern = defaultSchema["pattern"]?.ToString();
        schema.Format = defaultSchema["format"]?.ToString();
        if (Enum.TryParse<JsonSchemaType>(defaultTypes, ignoreCase: true, out var defaultSchemaType)) {
            schema.Type = defaultSchemaType;
        }
        if (schema.Type.HasValue && schema.Type.Value.HasFlag(JsonSchemaType.Number) && schema.Format is null) {
            if (typeof(double) == type || typeof(double?) == type ||
                typeof(decimal) == type || typeof(decimal?) == type) {
                schema.Format = "double";
            } else if (typeof(float) == type || typeof(float?) == type) {
                schema.Format = "float";
            }
        }
    }

    private static bool IsDictionatySchema(OpenApiSchema schema, Type type) {
        var isDictionary = schema.Type.HasValue && schema.Type!.Value.HasFlag(JsonSchemaType.Object) && (schema.Properties is null || schema.Properties?.Count == 0) &&
                               schema.AdditionalPropertiesAllowed == true &&
                               schema.AdditionalProperties is null &&
                               type.IsDictionary() && type.GenericTypeArguments.Length == 2 && type.GenericTypeArguments[1].IsPrimitive();
        return isDictionary;
    }

    private static void FixEmptyDictionaryItemSchema(OpenApiSchema schema, JsonSerializerOptions runtimeOptions, Type type) {
        var valueType = type.GenericTypeArguments[1];
        var valueSchema = new OpenApiSchema();
        var nullableType = Nullable.GetUnderlyingType(valueType!);
        bool nullable = nullableType != null;
        valueType = nullableType ?? valueType;
        TransformSchemaPrimitive(valueSchema, runtimeOptions, valueType);
        if (!nullable) {
            valueSchema.Type &= ~JsonSchemaType.Null;
        }
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = valueSchema;
    }

    private static JsonSerializerOptions StripCustomConverters(JsonSerializerOptions runtimeOptions) {
        var strippedOptions = new JsonSerializerOptions(runtimeOptions);
        for (var i = strippedOptions.Converters.Count - 1; i >= 0; i--) {
            var converter = strippedOptions.Converters[i];
            if (converter is not null && !converter.GetType().Namespace!.StartsWith("System", StringComparison.OrdinalIgnoreCase)) {
                strippedOptions.Converters.RemoveAt(i);
            }
        }
        return strippedOptions;
    }
}
#endif