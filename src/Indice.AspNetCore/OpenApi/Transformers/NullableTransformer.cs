#if NET10_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Microsoft.Extensions.DependencyInjection;
/// <summary>
/// This transformer attempts to coalesce nullable and non-nullable schemas by removing the null type
/// wherever nullability is already implied by the `required` property.
/// It also removes `null` from enum values if present.
/// Finally, it removes the "NullableOf" prefix from schema reference IDs if present, being careful to preserve
/// the original reference ID for non-nullable types.
/// </summary>
public static class NullableTransformer
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

    /// <summary>
    /// Adds a transformer to the OpenApiOptions that modifies schema properties to reflect nullability 
    /// </summary>
    /// <param name="options">The options to configure</param>
    /// <returns>The options for further configuration</returns>
    public static OpenApiOptions AddNullableTransformer(this OpenApiOptions options) {
        options.AddSchemaTransformer((schema, context, cancellationToken) => {
            if (schema.Properties is not null) {
                foreach (var property in schema.Properties) {
                    if (property.Value is OpenApiSchema propSchema) {
                        // Remove the null type for required properties
                        if (schema.Required?.Contains(property.Key) == true) {
                            if (propSchema.Type is not null) {
                                propSchema.Type &= ~JsonSchemaType.Null;
                            }
                            if (propSchema.OneOf is not null) {
                                var nullBranch = propSchema.OneOf.FirstOrDefault(s => s.Type == JsonSchemaType.Null);
                                if (nullBranch is not null) {
                                    propSchema.OneOf.Remove(nullBranch);
                                }
                                // If only one branch survives, collapse it into the parent so renderers don't show "oneOf [X]"
                                if (propSchema.OneOf.Count == 1 && propSchema.OneOf[0] is OpenApiSchema only) {
                                    propSchema.Type ??= only.Type;
                                    propSchema.Items ??= only.Items;
                                    propSchema.Format ??= only.Format;
                                    propSchema.OneOf.Clear();
                                }
                            }
                            propSchema.Metadata?.Remove("x-is-nullable-property");
                        }
                    }
                }
            }
            // Also need to remove `null` from enum values if present
            if (schema.Enum is not null && schema.Enum.Any(x => x is null)) {
                schema.Enum = schema.Enum
                    .Where(e => e is not null)
                    .ToList();
            }
            return Task.CompletedTask;
        });

        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;

        return options;
    }
}
#endif