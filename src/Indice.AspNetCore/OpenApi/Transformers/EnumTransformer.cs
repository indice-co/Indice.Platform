#if NET9_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
using Indice.Extensions;
using Indice.Serialization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Changes the OAS for enum flags and treats them as an array. This works in accordance with serialization by using the <see cref="JsonStringArrayEnumFlagsConverterFactory"/>.</summary>
internal static class EnumTransformer
{
    internal class ChainedDelegate(Func<JsonTypeInfo, string?> next)
    {
        public string? Invoke(JsonTypeInfo type) {
            // Get the result of the next delegate in the chain
            var result = next(type);
            if (result is null && type.Type.IsFlagsEnum()) {
                return type.Type.Name;
            }
            return result;
        }
    }
    public static OpenApiOptions AddEnumTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        //options.AddSchemaTransformer(TransformFlagsAsync);
        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;
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
        if (context.ParameterDescription?.Type.IsFlagsEnum() == true) {
            //TransformSchemaType(schema, context.ParameterDescription.Type);
        }
        if (context.JsonPropertyInfo?.PropertyType.IsFlagsEnum() == true) {
            TransformSchemaType(schema, context.JsonPropertyInfo.PropertyType);
        }
        return Task.CompletedTask;
    }

    private static void TransformSchemaType(OpenApiSchema schema, Type type) {
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (schema.OneOf.Count > 0) {
            return;
        }
        schema.OneOf = [
            new OpenApiSchema(schema),
            new OpenApiSchema() {
                Type = "array",
                Items = new OpenApiSchema(schema),
            }
        ];
        schema.Type = null;
        schema.Format = null;
        //schema.Nullable = context.JsonTypeInfo.Type.IsReferenceOrNullableType();
        schema.Enum?.Clear();
        schema.Annotations?.Clear();
    }

    private static bool IsReferenceOrNullableType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
#endif