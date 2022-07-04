using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema;
using NJsonSchema.Generation;

namespace Indice.Extensions
{
    /// <summary>Extension methods on <see cref="Type"/>.</summary>
    public static class TypeExtensions
    {
        /// <summary>Determines whether a type is decorated with <see cref="FlagsAttribute"/>.</summary>
        /// <param name="type">The type to check.</param>
        public static bool IsFlagsEnum(this Type type) => 
            (type.IsEnum && type.GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any()) || 
            (Nullable.GetUnderlyingType(type)?.IsEnum == true && Nullable.GetUnderlyingType(type).GetCustomAttributes(typeof(FlagsAttribute), inherit: false).Any());

        /// <summary>Generates JSON schema for a given C# class.</summary>
        /// <param name="type">Class type</param>
        /// <returns>A string containing JSON schema for a given class type.</returns>
        public static string ToJsonSchema(this Type type) {
            var schema = JsonSchema.FromType(type, new JsonSchemaGeneratorSettings { 
                SerializerSettings = new JsonSerializerSettings { 
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                }
            });
            return schema.ToJson();
        }
    }
}
