
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.
// original source code from: Microsoft.Azure.WebJobs.Host.Protocols

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Indice.Serialization
{
    /// <remarks>
    /// Unlike $type in JSON.NET, this converter decouples the message data from the .NET class and assembly names.
    /// It also allows emitting a type on the root object.
    /// </remarks>
    public class PolymorphicJsonConverter : JsonConverter
    {
        private readonly string _typePropertyName;
        private readonly IDictionary<string, Type> _nameToTypeMap;
        private readonly IDictionary<Type, string> _typeToNameMap;

        /// <summary>Initializes a new instance of the <see cref="PolymorphicJsonConverter"/> class.</summary>
        /// <param name="typeMapping">The type names to use when serializing types.</param>
        public PolymorphicJsonConverter(IDictionary<string, Type> typeMapping)
            : this("discriminator", typeMapping) {
        }

        /// <summary>Initializes a new instance of the <see cref="PolymorphicJsonConverter"/> class.</summary>
        /// <param name="typePropertyName">The name of the property in which to serialize the type name.</param>
        /// <param name="typeMapping">The type names to use when serializing types.</param>
        public PolymorphicJsonConverter(string typePropertyName, IDictionary<string, Type> typeMapping) {
            _typePropertyName = typePropertyName ?? throw new ArgumentNullException(nameof(typePropertyName));
            _nameToTypeMap = typeMapping ?? throw new ArgumentNullException(nameof(typeMapping));
            _typeToNameMap = new Dictionary<Type, string>();
            foreach (var item in _nameToTypeMap) {
                _typeToNameMap.Add(item.Value, item.Key);
            }
        }

        /// <summary>Gets the name of the property in which to serialize the type name.</summary>
        public string TypePropertyName {
            get { return _typePropertyName; }
        }
        /// <summary>Gets the name of the property in which to serialize the type name.</summary>
        public string JsonPropertyName {
            get {
                return new CamelCaseNamingStrategy(false, true).GetPropertyName(_typePropertyName, false);
            }
        }

        /// <inheritdoc />
        public override bool CanConvert(Type objectType) {
            return _typeToNameMap.ContainsKey(objectType);
        }

        /// <inheritdoc />
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader == null) throw new ArgumentNullException(nameof(reader)); 
            if (objectType == null) throw new NotSupportedException("Deserialization is not supported without specifying a default object Type.");
            if (serializer == null) throw new ArgumentNullException(nameof(serializer));
            if (reader.TokenType == JsonToken.Null) {
                return null;
            }

            var json = JToken.ReadFrom(reader);
            var typeToCreate = GetTypeToCreate(json) ?? objectType;
            var target = Activator.CreateInstance(typeToCreate);
            serializer.Populate(json.CreateReader(), target);
            return target;
        }

        /// <inheritdoc />
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            if (writer == null) throw new ArgumentNullException("writer");
            if (serializer == null) throw new ArgumentNullException("serializer");
            if (value == null) {
                writer.WriteNull();
                return;
            }

            var valueType = value.GetType();
            // Now that we've handled the type, temporarily remove this converter so we can serialize this element and
            // its children without infinite recursion.
            var originalContractResolver = serializer.ContractResolver;
            serializer.ContractResolver = new NonCircularContractResolver(valueType);

            var json = JObject.FromObject(value, serializer);

            var typeName = GetTypeName(valueType);
            if (typeName != null) {
                var jsonPropertyName = JsonPropertyName;
                if (json.Property(jsonPropertyName) != null) {
                    json.Remove(jsonPropertyName);
                }
                json.AddFirst(new JProperty(jsonPropertyName, typeName));
            }
            serializer.Serialize(writer, json);
            // Restore this converter so that subsequent siblings can use it.
            serializer.ContractResolver = originalContractResolver;
        }

        /// <summary>Gets all type name mappings in a type hierarchy.</summary>
        /// <returns>All type name mappings in the type hierarchy.</returns>
        public static IDictionary<string, Type> GetTypeMapping(Type baseType, string typePropertyName) {
            IDictionary<string, Type> typeMapping = new Dictionary<string, Type>();
            var options = new string[0];

#if NETSTANDARD14
            var discriminator = baseType.GetTypeInfo().GetDeclaredProperty(typePropertyName);
#else
            var discriminator = baseType.GetProperty(typePropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
#endif
            if (discriminator?.PropertyType
#if NETSTANDARD14
                .GetTypeInfo().IsEnum
#else
                .IsEnum
#endif
                == true) {
                options = Enum.GetNames(discriminator.PropertyType);
            }
            foreach (var type in GetTypesInHierarchy(baseType)) {
                var name = ResolveDiscriminatorValue(type, discriminator, options) ?? GetDeclaredTypeName(type);
                typeMapping.Add(name, type);
            }

            return typeMapping;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string FindDiscriminatorProperty<T>() {

            return typeof(T).GetRuntimeProperties().Where(x => x.PropertyType.GetTypeInfo().IsEnum)
                   .FirstOrDefault()?.Name;
        }

        /// <summary>
        /// Get the types in this hierarchy
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Type> GetTypesInHierarchy(Type baseType) {
            var baseTypeInfo = baseType.GetTypeInfo();
            return baseTypeInfo.Assembly.DefinedTypes.Where(t => !t.IsAbstract && baseTypeInfo.IsAssignableFrom(t)).Select(t => t.AsType());
        }

        private static string GetDeclaredTypeName(Type type) {
            Debug.Assert(type != null, "type must not be null");
            
            var attributes = (JsonObjectAttribute[])type.GetTypeInfo().GetCustomAttributes(typeof(JsonObjectAttribute), inherit: false);
            if (attributes != null && attributes.Length > 0) {
                return attributes[0].Id;
            }
            return type.Name;
        }


        private static string ResolveDiscriminatorValue(Type type, PropertyInfo discriminator, string[] options) {
            var value = default(string);
            if (discriminator == null || discriminator.Name.Equals("discriminator", StringComparison.OrdinalIgnoreCase)) {
                return null;
            }
            if (options != null && options.Length > 0) {
                try {
                    value = options.Where(name => type.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) > -1).Single();
                } catch {
                    value = discriminator.GetValue(Activator.CreateInstance(type), null).ToString();
                }
            } else {
                value = discriminator.GetValue(Activator.CreateInstance(type), null).ToString();
            }
            return value;
        }


        private string GetTypeName(Type type) {
            if (!_typeToNameMap.ContainsKey(type)) {
                return null;
            }

            return _typeToNameMap[type];
        }

        private Type GetTypeToCreate(JToken token) {
            var tokenObject = token as JObject;
            if (tokenObject == null) {
                return null;
            }
            var typeProperty = tokenObject.Property(JsonPropertyName);
            if (typeProperty == null) {
                return null;
            }
            var typeValue = typeProperty.Value as JValue;
            if (typeValue == null) {
                return null;
            }
            var typeString = typeValue.Value as string;
            if (typeString == null) {
                return null;
            }
            if (!_nameToTypeMap.ContainsKey(typeString)) {
                return null;
            }
            return _nameToTypeMap[typeString];
        }

        private class NonCircularContractResolver : DefaultContractResolver
        {
            private readonly Type _contractType;

            public NonCircularContractResolver(Type contractType) {
                Debug.Assert(contractType != null, "contract type must not be null");
                _contractType = contractType;
                NamingStrategy = new CamelCaseNamingStrategy {
                    OverrideSpecifiedNames = false
                };
            }

            protected override JsonContract CreateContract(Type objectType) {
                var contract = base.CreateContract(objectType);

                if (_contractType.GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo())) {
                    contract.Converter = null;
                }

                return contract;
            }
        }
    }
}
