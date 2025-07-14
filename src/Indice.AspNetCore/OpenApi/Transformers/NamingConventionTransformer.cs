#if NET9_0_OR_GREATER

using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Indice.Types;
using Microsoft.AspNetCore.OpenApi;

namespace Indice.AspNetCore.OpenApi.Transformers;
internal static class NamingConventionTransformer
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
    public static OpenApiOptions AddNamingConvensionTransformer(this OpenApiOptions options) {
        
        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;

        return options;
    }
}
#endif