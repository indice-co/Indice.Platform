#nullable enable
using System.Text.Json.Nodes;
using System.Text.Json;
using Json.Schema.Generation.Intents;
using Json.Schema.Generation;
using Json.Schema;
using Json.Schema.Generation.Generators;

namespace Indice.AspNetCore.Extensions;

/// <summary>Extensions related to <see cref="JsonSchema"/></summary>
public static class JsonSchemaNetExtensions
{
    /// <summary>Generates JSON schema for a given C# class using a new untested library :)</summary>
    /// <param name="type">Class type</param>
    /// <param name="nullability">If json schema will inclue nullable annotation for members. Defaults to <see cref="Nullability.AllowForNullableValueTypes"/></param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static JsonSchema ToJsonSchema(this Type type, Nullability nullability = Nullability.AllowForNullableValueTypes) {
        var configuration = new SchemaGeneratorConfiguration {
            PropertyNameResolver = Json.Schema.Generation.PropertyNameResolvers.CamelCase,
            Nullability = nullability,
        };
        configuration.Generators.Add(new EnumSchemaGenerator());
        configuration.Generators.Add(new DateTimeSchemaGenerator());
        var schema = new JsonSchemaBuilder().FromType(type, configuration).Build();
        return schema;
    }

    /// <summary>Serializes a JSON schema to <see cref="JsonElement"/>.</summary>
    /// <param name="schema">Class type</param>
    /// <returns>A <see cref="JsonElement"/> containing JSON schema for a given class type.</returns>
    public static JsonElement AsJsonElement(this JsonSchema schema) {
        return JsonSerializer.SerializeToElement(schema);
    }

    /// <summary>Serializes a JSON schema to <see cref="JsonNode"/>.</summary>
    /// <param name="schema">Class type</param>
    /// <returns>A <see cref="JsonNode"/> containing JSON schema for a given class type.</returns>
    public static JsonNode? AsJsonNode(this JsonSchema schema) {
        return JsonSerializer.SerializeToNode(schema);
    }

    /// <summary>Serializes a JSON schema to string.</summary>
    /// <param name="schema">Class type</param>
    /// <param name="options">Options to control the serialization behavior.</param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static string? AsJsonString(this JsonSchema schema, JsonSerializerOptions? options = null) {
        return JsonSerializer.SerializeToNode(schema)?.ToJsonString(options);
    }
}

internal class EnumSchemaGenerator : ISchemaGenerator
{
    public void AddConstraints(SchemaGenerationContextBase context) {
        var values = Enum.GetNames(context.Type).ToList();
        context.Intents.Add(new TypeIntent(SchemaValueType.String));
        context.Intents.Add(new EnumIntent(values));
    }
    public bool Handles(Type type) => type.IsEnum;
}

internal class DateTimeSchemaGenerator : ISchemaGenerator
{
    public bool Handles(Type type) => type == typeof(DateTime)
                                   || type == typeof(DateTimeOffset)
                                   || type == typeof(DateOnly)
                                   || Nullable.GetUnderlyingType(type) == typeof(DateTime)
                                   || Nullable.GetUnderlyingType(type) == typeof(DateTimeOffset)
                                   || Nullable.GetUnderlyingType(type) == typeof(DateOnly);

    public void AddConstraints(SchemaGenerationContextBase context) {
        context.Intents.Add(new TypeIntent(SchemaValueType.String));
        context.Intents.Add(new FormatIntent(Formats.DateTime));
    }
}

#nullable disable