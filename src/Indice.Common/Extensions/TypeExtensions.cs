using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        /// <summary>Determines whether a type is anonymous.</summary>
        /// <param name="type"></param>
        /// <returns>Returns true if type is anonymous, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="type"/> is null.</exception>
        public static bool IsAnonymousType(this Type type) {
            if (type == null) {
                throw new ArgumentNullException(nameof(type), "Parameter type cannot be null.");
            }
            return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
                && type.IsGenericType && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }
    }
}
