#if NET9_0_OR_GREATER
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Any;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;

namespace Microsoft.Extensions.DependencyInjection;
// This transformer attempts to coalesce nullable and non-nullable schemas by removing the `nullable` property
// wherever nullability is already implied by the `required` property.
// It also removes `null` from enum values if present.
// Finally, it removes the "NullableOf" prefix from schema reference IDs if present, being careful to preserve
// the original reference ID for non-nullable types.
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
                foreach (var property in schema.Properties) {
                    if (schema.Required?.Contains(property.Key) != true) {
                        property.Value.Nullable = false;
                    }
                    // Also need to remove `null` from enum values if present
                    if (property.Value.Enum is not null) {
                        property.Value.Enum = property.Value.Enum.Where(e => (e as OpenApiString)!.Value != null).ToList();
                    }
                    // And remove default value of null if set
                    if (property.Value.Default is OpenApiNull) {
                        property.Value.Default = null;
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