#if NET10_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using Indice.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This class provides a transformer for handling array schemas in OpenAPI documents.
/// </summary>
public static class CustomConverterSchemaTransformer
{
    private static readonly Type[] _converterTypes = [
        typeof(JsonStringDecimalConverter),
            typeof(JsonStringDoubleConverter),
            typeof(JsonStringInt32Converter),
            typeof(JsonStringBooleanConverter),
            typeof(JsonAnyStringConverter),
            typeof(TypeConverterJsonAdapterFactory)
];

    /// <summary>
    /// Adds a schema transformer to handle array transformations in OpenAPI options.
    /// </summary>
    /// <remarks>This method registers a schema transformer that processes array schemas in OpenAPI documents.
    /// It modifies the behavior of the OpenAPI generation pipeline to handle arrays in a specific way.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to which the transformer will be added.</param>
    /// <returns>The updated <see cref="OpenApiOptions"/> instance with the array transformer registered.</returns>
    public static OpenApiOptions AddCustomConverterTransformer(this OpenApiOptions options) {

        // Register the schema transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }


    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (schema.Type is null) {
            switch (context.JsonTypeInfo?.Converter) {
                case not null when context.JsonTypeInfo.Converter is JsonStringDecimalConverter:
                    schema.Type = JsonSchemaType.Number;
                    schema.Format = "decimal";
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is JsonStringDoubleConverter:
                    schema.Type = JsonSchemaType.Number;
                    schema.Format = "double";
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is JsonStringInt32Converter:
                    schema.Type = JsonSchemaType.Number;
                    schema.Format = "int32";
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is JsonStringBooleanConverter:
                    schema.Type = JsonSchemaType.Boolean;
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is JsonAnyStringConverter:
                    var defaultSchema = JsonSerializerOptions.Web.GetJsonSchemaAsNode(context.JsonTypeInfo.Type);
                    var isArrayOfTypes = defaultSchema["type"]!.GetValueKind() == JsonValueKind.Array;
                    var defaultTypes = isArrayOfTypes ? string.Join(',', defaultSchema["type"]!.AsArray().Select(x => x!.ToString())) : defaultSchema["type"]!.ToString();
                    if (Enum.TryParse<JsonSchemaType>(defaultTypes, ignoreCase: true, out var defaultSchemaType)) {
                        schema.Type = defaultSchemaType;
                    }
                    break;
                case not null when context.JsonTypeInfo.Converter is TypeConverterJsonAdapterFactory:
                    schema.Type = JsonSchemaType.String;
                    if (context.JsonPropertyInfo?.IsGetNullable == true) {
                        schema.Type |= JsonSchemaType.Null;
                    }
                    break;
                default:
                    break;
            }
        }
        if (schema.Type == JsonSchemaType.Array && schema.Items is null && context.JsonTypeInfo is not null) {
            var schemaItems = new OpenApiSchema();
            schema.Items = schemaItems;
            var defaultSchema = JsonSerializerOptions.Web.GetJsonSchemaAsNode(context.JsonTypeInfo.Type);
            var isArrayOfTypes = defaultSchema["items"]!["type"]!.GetValueKind() == JsonValueKind.Array;
            var defaultTypes = isArrayOfTypes ? string.Join(',', defaultSchema["items"]!["type"]!.AsArray().Select(x => x!.ToString())) : defaultSchema["type"]!.ToString();
            if (Enum.TryParse<JsonSchemaType>(defaultTypes, ignoreCase: true, out var defaultSchemaType)) {
                schemaItems.Type = defaultSchemaType;
                schemaItems.Type &= ~JsonSchemaType.Null;
            }
            if (context.JsonPropertyInfo?.IsGetNullable == true) {
                schema.Type |= JsonSchemaType.Null;
            }
        }
        return Task.CompletedTask;
    }
}
#endif