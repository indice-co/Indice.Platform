#if NET9_0_OR_GREATER
using Indice.Extensions;
using Indice.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Changes the OAS for enum flags and treats them as an array. This works in accordance with serialization by using the <see cref="JsonStringArrayEnumFlagsConverterFactory"/>.</summary>
internal static class EnumTransformer
{
    public static OpenApiOptions AddEnumTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        options.AddSchemaTransformer(TransformFlagsAsync);
        return options;
    }
    private static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        var type = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!type.IsEnum && !context.JsonTypeInfo.Type.IsFlagsEnum()) {
            return Task.CompletedTask;
        }
        var enumType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        schema.Type = "string";
        schema.Format = null;
        if (schema.Enum is null || schema.Enum.Count == 0) {
            schema.Enum = Enum.GetNames(enumType).Select(name => (IOpenApiAny)new OpenApiString(name)).ToList();
        }
        return Task.CompletedTask;
    }

    private static Task TransformFlagsAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (context.JsonPropertyInfo?.PropertyType.IsFlagsEnum() != true) {
            return Task.CompletedTask;
        }
        var enumType = Nullable.GetUnderlyingType(context.JsonPropertyInfo.PropertyType) ?? context.JsonPropertyInfo.PropertyType;
        //context.JsonPropertyInfo.CustomConverter = new JsonStringArrayEnumFlagsConverterFactory().CreateConverter(enumType, context.JsonPropertyInfo.Options);
        //oneOf:
        //    -type: string
        //  -type: array
        //    items:
        //      type: string
        schema.OneOf = [
            new OpenApiSchema(schema),
            new OpenApiSchema() {
                Type = "array",
                Items = new OpenApiSchema() {
                    Type = "string",
                    Annotations = new Dictionary<string, object>() {
                        ["x-schema-id"] = enumType.Name
                    },
                    Enum = [.. schema.Enum]
                },
            }
        ];
        schema.Type = null;
        schema.Format = null;
        schema.Nullable = context.JsonTypeInfo.Type.IsReferenceOrNullableType();
        schema.Enum?.Clear();
        schema.Annotations?.Clear();
        //new Dictionary<string, object>() {
        //    ["x-schema-id"] = enumType.Name
        //};
        return Task.CompletedTask;
    }
    private static bool IsReferenceOrNullableType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
#endif