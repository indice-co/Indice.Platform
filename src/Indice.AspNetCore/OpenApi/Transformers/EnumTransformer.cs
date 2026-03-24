#if NET10_0_OR_GREATER
using System.ComponentModel;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Changes the OAS for enums.</summary>
public static class EnumTransformer
{
    /// <summary>
    /// Adds a transformer to the OpenApiOptions that modifies enum schemas to reflect enum values and names.
    /// </summary>
    /// <param name="options">The options to configure.</param>
    /// <returns>The options for further configuration.</returns>
    public static OpenApiOptions AddEnumTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer(TransformAsync);
        //var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        //options.CreateSchemaReferenceId = chainedDelegate.Invoke;
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
            
        }
        return Task.CompletedTask;
    }

    private static bool TryFindMvcQueryParameterEnumType(OpenApiSchema schema, ApiParameterDescription? parameterDescription, out Type? modelType) {
        modelType = null;
        if (parameterDescription?.ModelMetadata is Microsoft.AspNetCore.Mvc.ModelBinding.Metadata.DefaultModelMetadata mvcModelMetadata &&
            mvcModelMetadata.IsEnum &&
            schema.Enum!.Count == 0) {
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
        if (!enumType.IsEnum || schema.Extensions?.Count > 0) {
            return false;
        }

        var isString = schema.Enum?.FirstOrDefault()?.ToJsonString().StartsWith('"') ??
                       schema.Type?.HasFlag(JsonSchemaType.String) ?? 
                       context.JsonTypeInfo.Options.Converters.OfType<JsonStringEnumConverter>().Any();
        if (!schema.Type.HasValue) {
            schema.Type = isString ? JsonSchemaType.String : JsonSchemaType.Integer;
        }
        
        schema.Format = null;
        var fields = enumType.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(x => x.Name, x => new {
            Name = x.GetCustomAttribute<JsonStringEnumMemberNameAttribute>()?.Name ?? x.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? x.Name,
            x.GetCustomAttribute<DescriptionAttribute>()?.Description
        });
        var enumNames = Enum.GetNames(enumType);
        var enumValues = Enum.GetValuesAsUnderlyingType(enumType).Cast<object>().Select(Convert.ToInt32).ToArray();
        var openApiValueArray = new List<JsonNode>();
        var openApiNameArray = new List<JsonNode>();
        var openApiDescArray = new List<JsonNode>();
        bool writeDescriptions = false;
        for (int i = 0; i < enumValues.Length; i++) {
            openApiValueArray.Add(isString ? (JsonNode)fields[enumNames[i]].Name! : (JsonNode)enumValues[i]);
            openApiNameArray.Add(enumNames[i]);
            openApiDescArray.Add(fields[enumNames[i]].Description!);
            writeDescriptions |= !string.IsNullOrWhiteSpace(fields[enumNames[i]].Description);
        }
        schema.Extensions ??= new Dictionary<string, IOpenApiExtension>();
        schema.Extensions!.Add("x-enum-varnames", new EnumNamesOpenApiExtension(openApiNameArray));
        if (writeDescriptions) {
            schema.Extensions.Add("x-enum-descriptions", new EnumNamesOpenApiExtension(openApiDescArray));
        }
        schema.Enum = openApiValueArray;
        return true;
    }

    private static bool IsReferenceOrNullableType(this Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;
}

internal class EnumNamesOpenApiExtension : IOpenApiExtension
{
    public EnumNamesOpenApiExtension(List<JsonNode> enumDescriptions) {
        EnumDescriptions = enumDescriptions;
    }

    public List<JsonNode> EnumDescriptions { get; }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion) {
        if (writer is null) {
            throw new ArgumentNullException(nameof(writer));
        }
        writer.WriteStartArray();
        foreach (var description in EnumDescriptions) {
            writer.WriteValue(description.ToString());
        }
        writer.WriteEndArray();
    }
}
#endif