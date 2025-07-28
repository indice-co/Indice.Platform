#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This transformer attempts to coalesce nullable and non-nullable schemas by removing the `nullable` property
/// wherever nullability is already implied by the `required` property.
/// It also removes `null` from enum values if present.
/// Finally, it removes the "NullableOf" prefix from schema reference IDs if present, being careful to preserve
/// the original reference ID for non-nullable types.
/// </summary>
internal static class NullableTransformer
{
    internal class ChainedDelegate(Func<JsonTypeInfo, string?> next)
    {
        public string? Invoke(JsonTypeInfo type) {
            // Get the result of the next delegate in the chain
            var result = next(type);
            // remove the "NullableOf" prefix for nullable types if present
            if (result is not null && type.Type.IsGenericType && type.Type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                result = Regex.Replace(result, "^NullableOf", "");
            }
            return result;
        }
    }

    public static OpenApiOptions AddNullableTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer((schema, context, cancellationToken) => {
            if (schema.Properties is not null) {
                foreach (var jsonProperty in context.JsonTypeInfo.Properties) {
                    if (!schema.Properties.TryGetValue(jsonProperty.Name, out var property)) {
                        continue;
                    }
                    if (schema.Required?.Contains(jsonProperty.Name) != true) {
                        property!.Nullable = false;
                    }
                    var nullableType = Nullable.GetUnderlyingType(jsonProperty.PropertyType);
                   
                    property!.Nullable = (nullableType is not null) || jsonProperty.IsGetNullable;
                    property.Type ??= (nullableType ?? jsonProperty.PropertyType).Name switch
                    {
                        "Int32" => "integer",
                        "Int64" => "integer",
                        "Double" => "number",
                        "Single" => "number",
                        "Decimal" => "number",
                        "Boolean" => "boolean",
                        "String" => "string",
                        "DateTime" => "string",
                        "DateTimeOffset" => "string",
                        "Guid" => "string",
                        _ => null
                    }; 
                    property.Format ??= (nullableType ?? jsonProperty.PropertyType).Name switch {
                        "Int32" => "int32",
                        "Int64" => "int64",
                        "Double" => "double",
                        "Single" => "float",
                        "Decimal" => "double",
                        "Boolean" => null,
                        "String" => null,
                        "DateTime" => "date-time",
                        "DateTimeOffset" => "date-time",
                        "Guid" => "uuid",
                        _ => null
                    };
                    if (property!.Annotations?.Any(x => x.Value != null) == true) {
                        property.Nullable = false;
                    }
                    // Also need to remove `null` from enum values if present
                    if (property.Enum is not null) {
                        property.Enum = property.Enum.Where(e => (e as OpenApiString)!.Value != null).ToList();
                    }
                    // And remove default value of null if set
                    if (property.Default is OpenApiNull) {
                        property.Default = null;
                    }
                }
            }
            return Task.CompletedTask;
        });

        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;

        return options;
    }
}
#endif