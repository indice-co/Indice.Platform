#if NET10_0_OR_GREATER
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides functionality for mapping .NET types to OpenAPI schemas and transforming OpenAPI schemas based on
/// predefined mappings.
/// </summary>
/// <remarks>This class allows developers to define custom mappings between .NET types and OpenAPI schemas,
/// enabling fine-grained control over how types are represented in OpenAPI documentation. It also provides methods to
/// apply these mappings during schema generation and transformation.</remarks>
public static class MappedTypeTransformer
{
    internal class ChainedDelegate(Func<JsonTypeInfo, string?> next)
    {
        public string? Invoke(JsonTypeInfo type) {
            // Get the result of the next delegate in the chain
            var result = next(type);
            // reverse the "ResultSetOf" prefix for generic types if present so that the schema reference ID is more readable as MyTypeResultSet.
            if (!string.IsNullOrWhiteSpace(result) && transforms.ContainsKey(type.Type)) {
                return null;
            }
            if (!string.IsNullOrWhiteSpace(result) && renames.ContainsKey(type.Type)) {
                return renames[type.Type];
            }
            return result;
        }
    }

    internal static Dictionary<Type, OpenApiSchema> transforms = new();
    internal static Dictionary<Type, string> renames = new();

    /// <summary>
    /// Maps a specified type to an OpenAPI schema definition.
    /// </summary>
    /// <remarks>This method associates the specified type <typeparamref name="T"/> with the given OpenAPI
    /// schema in order to replace its occurances in the open api document.</remarks>
    /// <typeparam name="T">The type to be mapped to the provided OpenAPI schema.</typeparam>
    /// <param name="schema">The <see cref="OpenApiSchema"/> instance representing the schema definition for the type <typeparamref
    /// name="T"/>.</param>
    public static void MapType<T>(OpenApiSchema schema) {
        transforms[typeof(T)] = schema;
    }

    /// <summary>
    /// Rename a specified type in the OpenAPI schema reference ID generation process. 
    /// </summary>
    /// <typeparam name="T">The type occurance to rename</typeparam>
    /// <param name="schemaName">The new name for the type</param>
    public static void RenameType<T>(string schemaName) {
        renames[typeof(T)] = schemaName;
    }


    /// <summary>
    /// Configures the <see cref="OpenApiOptions"/> instance with predefined type mappings and a schema transformer.
    /// </summary>
    /// <remarks>This method maps several common .NET types to their corresponding OpenAPI schema
    /// representations. It also registers a schema transformer to further customize the OpenAPI schema generation
    /// process.</remarks>
    /// <param name="options">The <see cref="OpenApiOptions"/> instance to configure.</param>
    /// <returns>The configured <see cref="OpenApiOptions"/> instance.</returns>
    public static OpenApiOptions AddMappedTypeTransformer(this OpenApiOptions options) {
        MapType<object>(new() { Type = JsonSchemaType.Object | JsonSchemaType.Null });
        MapType<JsonNode>(new() { Type = JsonSchemaType.Object | JsonSchemaType.Null });
        MapType<JsonElement>(new() { Type = JsonSchemaType.Object });
        MapType<JsonElement?>(new() { Type = JsonSchemaType.Object | JsonSchemaType.Null });
        MapType<GeoPoint>(new() { Type = JsonSchemaType.String });
        MapType<GeoPoint?>(new() { Type = JsonSchemaType.String | JsonSchemaType.Null });
        MapType<FilterClause>(new() { Type = JsonSchemaType.String });
        MapType<FilterClause?>(new() { Type = JsonSchemaType.String | JsonSchemaType.Null });
        MapType<Base64Id>(new() { Type = JsonSchemaType.String });
        MapType<Base64Id?>(new() { Type = JsonSchemaType.String | JsonSchemaType.Null });
        MapType<GuidOrAlias>(new() { Type = JsonSchemaType.String });
        MapType<GuidOrAlias?>(new() { Type = JsonSchemaType.String | JsonSchemaType.Null });
        MapType<Base64Host>(new() { Type = JsonSchemaType.String });
        MapType<Base64Host?>(new() { Type = JsonSchemaType.String | JsonSchemaType.Null });
        // Register the type transformer
        RenameType<Stream>("FileParam");
        RenameType<IFormFile>("FileParam");
        //MapType<Stream>(new() { Type = JsonSchemaType.String, Format = "binary" });
        //MapType<IFormFile>(new() { Type = JsonSchemaType.String, Format = "binary" });


        var chainedDelegate = new ChainedDelegate(options.CreateSchemaReferenceId);
        options.CreateSchemaReferenceId = chainedDelegate.Invoke;
        options.AddSchemaTransformer(TransformAsync);
        return options;
    }

    internal static Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken) {
        // If transforms contains the schema's type, set the schema type and format from the transform schema
        if (transforms.TryGetValue(context.JsonTypeInfo.Type, out var apiSchema) && schema.Metadata?.ContainsKey("mapped") != true) {
            TransformSchema(schema, context.JsonTypeInfo.Type, nullable: context.JsonPropertyInfo?.IsSetNullable);
            return Task.CompletedTask;
        }
        if (context.ParameterDescription is not null &&
            transforms.ContainsKey(context.ParameterDescription.Type)) {
            TransformSchema(schema, context.ParameterDescription.Type, nullable: null);
            return Task.CompletedTask;
        }
        return Task.CompletedTask;
    }

    private static void TransformSchema(OpenApiSchema schema, Type type, bool? nullable) {
        OpenApiSchema transformedSchema = transforms[type];
        schema.Type = transformedSchema.Type;
        schema.Format = transformedSchema.Format;
        schema.AnyOf = null;
        schema.AnyOf = null;
        schema.Metadata ??= new Dictionary<string, object>();
        schema.Metadata.Add("mapped", true);
        if (nullable == false) {
            schema.Type &= ~JsonSchemaType.Null;
        }
    }
}
#endif