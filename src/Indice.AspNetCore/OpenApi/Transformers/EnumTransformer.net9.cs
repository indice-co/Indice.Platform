#if NET9_0
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Indice.Extensions;
using Indice.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Changes the OAS for enum flags and treats them as an array. This works in accordance with serialization by using the <see cref="JsonStringArrayEnumFlagsConverterFactory"/>.</summary>
public static class EnumTransformer
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

    /// <summary>
    /// Adds a transformer to the OpenApiOptions that modifies enum schemas to reflect enum values and names.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    /// <returns>The options for further configuration.</returns>
    public static OpenApiOptions AddEnumTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;
        return options;
    }

    /// <summary>
    /// Transforms the OpenAPI schema for enum types by adding enum values, names, and descriptions as extensions to the schema.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to transform.</param>
    /// <param name="context">The context containing information about the schema transformation, including the JSON type information and parameter description.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        if (TryTransformEnum(schema, context, context.JsonTypeInfo.Type)) {
            return Task.CompletedTask;
        }

        if (TryFindMvcQueryParameterEnumType(schema, context.ParameterDescription, out var modelType) && TryTransformEnum(schema, context, modelType!)) {
            schema.Annotations ??= new Dictionary<string, object>();
            schema.Annotations["x-schema-id"] = (Nullable.GetUnderlyingType(modelType!) ?? modelType!).Name;
        }
        return Task.CompletedTask;
    }

    private static bool TryFindMvcQueryParameterEnumType(OpenApiSchema schema, ApiParameterDescription? parameterDescription, out Type? modelType) {
        modelType = null;
        if (parameterDescription?.ModelMetadata is Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata mvcModelMetadata &&
            mvcModelMetadata.IsEnum &&
            schema.Enum.Count == 0) {
            modelType = mvcModelMetadata.ModelType;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts to transform the OpenAPI schema for an enum type by adding enum values, names, and descriptions as extensions to the schema.
    /// </summary>
    /// <param name="schema">The OpenAPI schema to transform.</param>
    /// <param name="context">The context containing information about the schema transformation, including the JSON type information and parameter description.</param>
    /// <param name="type">The type to check for being an enum and to extract enum values, names, and descriptions from.</param>
    /// <returns>true if transformed</returns>
    public static bool TryTransformEnum(OpenApiSchema schema, OpenApiSchemaTransformerContext context, Type type) {
        
        var enumType = Nullable.GetUnderlyingType(type) ?? type;
        if (!enumType.IsEnum || schema.Extensions.Count > 0) {
            return false;
        }
        var isString = context.JsonTypeInfo.Options.Converters.OfType<JsonStringEnumConverter>().Any();
        var underlyingType = Enum.GetUnderlyingType(enumType);
        var isLong = underlyingType.Name.ToLowerInvariant().Equals("int64");
        schema.Type = isString ? "string" : "integer";
        schema.Format = null;
        if (!isString && isLong) { 
            schema.Format = "int64";
        }
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(x => x.Name, x => new {
            Name = x.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? x.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? x.Name,
            x.GetCustomAttribute<DescriptionAttribute>()?.Description
        });
        var enumNames = Enum.GetNames(enumType);
        var enumValues = isLong ? Enum.GetValuesAsUnderlyingType(enumType)! : Enum.GetValuesAsUnderlyingType(enumType).Cast<object>().Select(Convert.ToInt32).ToArray()!;
        var openApiValueArray = new OpenApiArray();
        var openApiNameArray = new OpenApiArray();
        var openApiDescArray = new OpenApiArray();
        bool writeDescriptions = false;
        for (int i = 0; i < enumValues.Length; i++) {
            openApiValueArray.Add(isString ? new OpenApiString(fields[enumNames[i]].Name) :
                                  isLong ? new OpenApiLong(((long[])enumValues)[i]) : 
                                           new OpenApiInteger(((int[])enumValues)[i]));
            openApiNameArray.Add(new OpenApiString(enumNames[i]));
            openApiDescArray.Add(new OpenApiString(fields[enumNames[i]].Description));
            writeDescriptions |= !string.IsNullOrWhiteSpace(fields[enumNames[i]].Description);
        }
        schema.Extensions.Add("x-enum-varnames", openApiNameArray);
        if (writeDescriptions) {
            schema.Extensions.Add("x-enum-descriptions", openApiDescArray);
        }
        schema.Enum = openApiValueArray;

        return true;
    }

    private static bool IsReferenceOrNullableType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}
#endif