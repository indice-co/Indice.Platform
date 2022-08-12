using System;
using System.Collections.Generic;
using System.Text;
using Json.Schema.Generation;
using Json.Schema;
using System.Linq;
using Json.More;
using Json.Pointer;
using Json.Schema.Generation.Generators;
using System.Text.Json;

namespace Indice.Extensions
{
    /// <summary>
    /// Extensions related to <see cref="JsonSchema"/>
    /// </summary>
    public static class JsonSchemaNetExtensions
    {
        /// <summary>
        /// Generates JSON schema for a given C# class using a new untested library :)
        /// </summary>
        /// <param name="type">Class type</param>
        /// <returns>A string containing JSON schema for a given class type.</returns>
        public static JsonSchema ToJsonSchema(this Type type) {
            var configuration = new SchemaGeneratorConfiguration {
                
                PropertyNamingMethod = PropertyNamingMethods.CamelCase
            };
            configuration.Generators.Add(new EnumSchemaGenerator());
            var schema = new JsonSchemaBuilder().FromType(type, configuration).Build();
            return schema;
        }

        /// <summary>
        /// Serializes a JSON schema to element.
        /// </summary>
        /// <param name="schema">Class type</param>
        /// <returns>A string containing JSON schema for a given class type.</returns>
        public static JsonElement AsJsonElement(this JsonSchema schema) {
            return JsonSerializer.SerializeToElement(schema);
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

    internal class ExtendedSchemaKeywordIntent : ISchemaKeywordIntent
    {
        public ExtendedSchemaKeywordIntent(IEnumerable<string> names) : this(names.ToArray()) { }

        public ExtendedSchemaKeywordIntent(params string[] names) => Names = names.ToList();

        public List<string> Names { get; set; }

        public void Apply(JsonSchemaBuilder builder) {
            builder.Type(SchemaValueType.String);
            builder.Enum(Names.Select(x => x.AsJsonElement().AsNode()));
        }

        public override bool Equals(object obj) => obj is not null;

        public override int GetHashCode() {
            unchecked {
                var hashCode = GetType().GetHashCode();
                hashCode = (hashCode * 397) ^ Names.GetCollectionHashCode();
                return hashCode;
            }
        }
    }
}
