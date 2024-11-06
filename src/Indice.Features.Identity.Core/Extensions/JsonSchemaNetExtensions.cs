﻿using System.Text.Json;
using System.Text.Json.Nodes;
using Json.More;
using Json.Pointer;
using Json.Schema;
using Json.Schema.Generation;
using Json.Schema.Generation.Generators;
using Json.Schema.Generation.Intents;

namespace Indice.Features.Identity.Core.Extensions;

/// <summary>Extensions related to <see cref="JsonSchema"/></summary>
public static class JsonSchemaNetExtensions
{
    /// <summary>Generates JSON schema for a given C# class using a new untested library :)</summary>
    /// <param name="type">Class type</param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static JsonSchema ToJsonSchema(this Type type) {
        var configuration = new SchemaGeneratorConfiguration {
            PropertyNameResolver = Json.Schema.Generation.PropertyNameResolvers.CamelCase,
            Nullability = Nullability.AllowForAllTypes,
        };
        configuration.Generators.Add(new EnumSchemaGenerator());
        configuration.Generators.Add(new DateTimeSchemaGenerator());
        var schema = new JsonSchemaBuilder().FromType(type, configuration).Build();
        return schema;
    }

    /// <summary>Serializes a JSON schema to element.</summary>
    /// <param name="schema">Class type</param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static JsonElement AsJsonElement(this JsonSchema schema) {
        return JsonSerializer.SerializeToElement(schema);
    }

    /// <summary>Serializes a JSON schema to element.</summary>
    /// <param name="schema">Class type</param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static JsonNode? AsJsonNode(this JsonSchema schema) {
        return JsonSerializer.SerializeToNode(schema);
    }

    /// <summary>Serializes a JSON schema to element.</summary>
    /// <param name="schema">Class type</param>
    /// <returns>A string containing JSON schema for a given class type.</returns>
    public static string? AsJsonString(this JsonSchema schema) {
        return JsonSerializer.SerializeToNode(schema)?.ToJsonString();
    }
}

internal class EnumSchemaGenerator : ISchemaGenerator
{
    public void AddConstraints(SchemaGenerationContextBase context) {
        var values = Enum.GetNames(context.Type).ToList();
        context.Intents.Add(new ExtendedSchemaKeywordIntent(values));
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

internal class ExtendedSchemaKeywordIntent : ISchemaKeywordIntent
{
    public ExtendedSchemaKeywordIntent(IEnumerable<string> names) : this(names.ToArray()) { }

    public ExtendedSchemaKeywordIntent(params string[] names) => Names = names.ToList();

    public List<string> Names { get; set; }

    public void Apply(JsonSchemaBuilder builder) {
        builder.Type(SchemaValueType.String);
        builder.Enum(Names.Select(x => x.AsJsonElement().AsNode()));
    }

    public override bool Equals(object? obj) => obj is not null;

    public override int GetHashCode() {
        unchecked {
            var hashCode = GetType().GetHashCode();
            hashCode = (hashCode * 397) ^ Names.GetCollectionHashCode();
            return hashCode;
        }
    }
}
