#if NET9_0_OR_GREATER
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
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
        //if (schema.Type == "array" && context.JsonTypeInfo.ElementType?.IsEnum == true) {
        //    schema.Items.Annotations ??= new Dictionary<string, object>();
        //    schema.Items.Annotations?.Clear();
        //    schema.Items.Annotations!.Add("x-schema-id", context.JsonTypeInfo.ElementType.Name);
        //}
        var enumType = Nullable.GetUnderlyingType(context.JsonTypeInfo.Type) ?? context.JsonTypeInfo.Type;
        if (!enumType.IsEnum || schema.Extensions.Count > 0) {
            return Task.CompletedTask;
        }
        var isString = context.JsonTypeInfo.Options.Converters.OfType<JsonStringEnumConverter>().Any();

        schema.Type = isString ? "string" : "integer";
        schema.Format = null;
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(x => x.Name, x => new {
            Name = x.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? x.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? x.Name,
            x.GetCustomAttribute<DescriptionAttribute>()?.Description
        });
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValuesAsUnderlyingType(enumType).Cast<object>().Select(Convert.ToInt32).ToArray();
        var openApiValueArray = new OpenApiArray();
        var openApiNameArray = new OpenApiArray();
        var openApiDescArray = new OpenApiArray();
        bool writeDescriptions = false;
        for (int i = 0; i < enumValues.Length; i++) {
            openApiValueArray.Add(isString ? new OpenApiString(fields[enumNames[i]].Name) : new OpenApiInteger(enumValues[i]));
            openApiNameArray.Add(new OpenApiString(enumNames[i]));
            openApiDescArray.Add(new OpenApiString(fields[enumNames[i]].Description));
            writeDescriptions |= !string.IsNullOrWhiteSpace(fields[enumNames[i]].Description);
        }
        schema.Extensions.Add("x-enum-varnames", openApiNameArray);
        if (writeDescriptions) {
            schema.Extensions.Add("x-enum-descriptions", openApiDescArray);
        }
        schema.Enum = openApiValueArray;

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