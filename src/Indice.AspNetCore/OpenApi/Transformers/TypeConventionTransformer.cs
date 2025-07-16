#if NET9_0_OR_GREATER

using System.Collections;
using System.Data;
using System.Security.Cryptography.Xml;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Indice.Types;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Indice.AspNetCore.OpenApi.Transformers;
internal static class TypeConventionTransformer
{
    internal class ChainedDelegate(Func<JsonTypeInfo, string?> next)
    {
        public string? Invoke(JsonTypeInfo type) {
            // Get the result of the next delegate in the chain
            var result = next(type);
            // reverse the "ResultSetOf" prefix for generic types if present so that the schema reference ID is more readable as MyTypeResultSet.
            if (result is not null && type.Type.IsGenericType && type.Type.GetGenericTypeDefinition() == typeof(ResultSet<>)) {
                result = Regex.Replace(result, "^ResultSetOf(.+)", "$1ResultSet");
            }
            return result;
        }
    }
    public static OpenApiOptions AddConventionsTransformer(this OpenApiOptions options) {

        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;

        // Register the schema transformer
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }


    public static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        // If transforms contains the schema's type, set the schema type and format from the transform schema
        if (CanTransform(schema, context.JsonTypeInfo.Type)) {
            schema.AdditionalPropertiesAllowed = false;
        }
        if (schema.Type == "array" && string.IsNullOrEmpty(schema.Items?.Type)) {
            var itemSchema = new OpenApiSchema() { };
            // element type switch.

            switch (context.JsonTypeInfo.ElementType) {
                case Type t when t == typeof(int):
                    itemSchema.Type = "integer";
                    itemSchema.Format = "int32";
                    break;
                case Type t when t == typeof(decimal) || t == typeof(double):
                    itemSchema.Type = "number";
                    itemSchema.Format = "double";
                    break;
                case Type t when t == typeof(DateTime):
                    itemSchema.Type = "string";
                    itemSchema.Format = "date-time";
                    break;
                case Type t when t == typeof(string):
                    itemSchema.Type = "string";
                    break;

                case Type t when t.IsEnum:
                    itemSchema.Annotations = new Dictionary<string, object> {
                        ["x-schema-key"] = t.Name
                    };
                    break;
                default:
                    
                    break;
            }
            schema.Items = new OpenApiSchema(itemSchema);
        }
        return Task.CompletedTask;
    }

    private static bool CanTransform(OpenApiSchema schema, Type? type) {
        if (type is null) {
            return false;
        }
        type = Nullable.GetUnderlyingType(type) ?? type;
        return schema.Type == "object" && schema.Properties.Count > 0 && type is not null && !type.IsPrimitive() && !type.IsDictionary() && type.Namespace?.StartsWith("Microsoft", StringComparison.OrdinalIgnoreCase) != true;
    }
}
#endif