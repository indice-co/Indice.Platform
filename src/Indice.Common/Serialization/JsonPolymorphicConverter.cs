using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// A JSON converter that helps serialize and deserialize types that make use of inheritance.
    /// </summary>
    public class JsonPolymorphicConverter : JsonConverter<object>
    {
        private readonly IDictionary<string, Type> _nameToTypeMap;
        private readonly IDictionary<Type, string> _typeToNameMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetPolymorphicConverter"/> class.
        /// </summary>
        /// <param name="typeMapping">The type names to use when serializing types.</param>
        public JsonPolymorphicConverter(IDictionary<string, Type> typeMapping) : this("discriminator", typeMapping) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetPolymorphicConverter"/> class.
        /// </summary>
        /// <param name="typePropertyName">The name of the property in which to serialize the type name.</param>
        /// <param name="typeMapping">The type names to use when serializing types.</param>
        public JsonPolymorphicConverter(string typePropertyName, IDictionary<string, Type> typeMapping) {
            TypePropertyName = typePropertyName ?? throw new ArgumentNullException(nameof(typePropertyName));
            _nameToTypeMap = typeMapping ?? throw new ArgumentNullException(nameof(typeMapping));
            _typeToNameMap = new Dictionary<Type, string>();
            foreach (var item in _nameToTypeMap) {
                _typeToNameMap.Add(item.Value, item.Key);
            }
        }

        /// <summary>
        /// Gets the name of the property in which to serialize the type name.
        /// </summary>
        public string TypePropertyName { get; private set; }

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) {
            var contained = _typeToNameMap.ContainsKey(typeToConvert);
            return contained;
        }

        /// <inheritdoc />
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            if (reader.TokenType != JsonTokenType.StartObject) {
                throw new JsonException();
            }
            if (typeToConvert == null) {
                throw new NotSupportedException("Deserialization is not supported without specifying a default object type.");
            }
            if (reader.TokenType == JsonTokenType.Null) {
                return null;
            }
            var canParse = JsonDocument.TryParseValue(ref reader, out var jsonDocument);
            if (!canParse) {
                throw new JsonException();
            }
            var jsonPropertyName = options.PropertyNamingPolicy.ConvertName(TypePropertyName);
            jsonDocument.RootElement.TryGetProperty(jsonPropertyName, out var jsonElement);
            var propertyValue = jsonElement.ValueKind != JsonValueKind.Undefined ? jsonElement.GetString() : string.Empty;
            var typeToCreate = _nameToTypeMap.ContainsKey(propertyValue) ? _nameToTypeMap[propertyValue] : typeToConvert;
            var rawJson = jsonDocument.RootElement.GetRawText();
            var jsonSerializerOptions = CopyJsonSerializerOptions(options);
            var target = JsonSerializer.Deserialize(rawJson, typeToCreate, jsonSerializerOptions);
            return target;
        }

        /// <inheritdoc />
        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options) {
            if (writer == null) {
                throw new ArgumentNullException(nameof(writer));
            }
            if (value == null) {
                writer.WriteNullValue();
                return;
            }
            var valueType = value.GetType();
            // Serializer options cannot be changed once serialization or deserialization has occurred,
            // so we have to copy serializer options to a new object and alter it.
            var jsonSerializerOptions = CopyJsonSerializerOptions(options);
            // Don't pass in the options object when recursively calling Serialize or Deserialize. 
            // The options object contains the Converters collection. If you pass it in to Serialize or Deserialize, the custom converter calls into itself, 
            // making an infinite loop that results in a stack overflow exception.
            JsonSerializer.Serialize(writer, value, valueType, jsonSerializerOptions);
        }

        /// <summary>
        /// Gets all type name mappings in a type hierarchy.
        /// </summary>
        /// <returns>All type name mappings in the type hierarchy.</returns>
        public static IDictionary<string, Type> GetTypeMapping(Type baseType, string typePropertyName) {
            IDictionary<string, Type> typeMapping = new Dictionary<string, Type>();
            var options = Array.Empty<string>();
            var discriminator = default(PropertyInfo);
            if (!string.IsNullOrWhiteSpace(typePropertyName)) {
                discriminator = baseType.GetProperty(typePropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            if (discriminator?.PropertyType.IsEnum == true) {
                options = Enum.GetNames(discriminator.PropertyType);
            }
            foreach (var type in GetTypesInHierarchy(baseType)) {
                var name = ResolveDiscriminatorValue(type, discriminator, options) ?? type.Name;
                typeMapping.Add(name, type);
            }
            return typeMapping;
        }

        /// <summary>
        /// Finds the property name that serves as a discriminator so to discover the concrete type.
        /// </summary>
        /// <typeparam name="T">The type of the base class.</typeparam>
        protected static string FindDiscriminatorProperty<T>() {
            var discriminatorProperty = typeof(T).GetRuntimeProperties().Where(x => x.PropertyType.GetTypeInfo().IsEnum).FirstOrDefault()?.Name;
            return discriminatorProperty;
        }

        /// <summary>
        /// Get the types in this hierarchy.
        /// </summary>
        private static IEnumerable<Type> GetTypesInHierarchy(Type baseType) {
            var baseTypeInfo = baseType.GetTypeInfo();
            return baseTypeInfo.Assembly.DefinedTypes.Where(type => !type.IsAbstract && baseTypeInfo.IsAssignableFrom(type)).Select(t => t.AsType());
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

        private JsonSerializerOptions CopyJsonSerializerOptions(JsonSerializerOptions options) {
            var jsonSerializerOptions = new JsonSerializerOptions {
                AllowTrailingCommas = options.AllowTrailingCommas,
                DefaultBufferSize = options.DefaultBufferSize,
                DictionaryKeyPolicy = options.DictionaryKeyPolicy,
                Encoder = options.Encoder,
                IgnoreNullValues = options.IgnoreNullValues,
                IgnoreReadOnlyProperties = options.IgnoreReadOnlyProperties,
                MaxDepth = options.MaxDepth,
                PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive,
                PropertyNamingPolicy = options.PropertyNamingPolicy,
                ReadCommentHandling = options.ReadCommentHandling,
                WriteIndented = options.WriteIndented
            };
            foreach (var converter in options.Converters) {
                var converterType = converter.GetType();
                if (!(converterType.IsGenericType && converterType.GetGenericTypeDefinition().IsAssignableFrom(typeof(JsonPolymorphicConverterFactory<>)))) {
                    jsonSerializerOptions.Converters.Add(converter);
                }
            }
            return jsonSerializerOptions;
        }
    }

    /// <summary>
    /// Converts an object to and from JSON.
    /// </summary>
    /// <typeparam name="T">The type of the base class in the inheritance hierarchy.</typeparam>
    public class JsonPolymorphicConverter<T> : JsonPolymorphicConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetPolymorphicConverter{T}"/> class.
        /// </summary>
        public JsonPolymorphicConverter() : this(FindDiscriminatorProperty<T>()) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonNetPolymorphicConverter{T}"/> class.
        /// </summary>
        /// <param name="typePropertName">The name of the property in which to serialize the type name.</param>
        public JsonPolymorphicConverter(string typePropertName) : base(typePropertName, GetTypeMapping(typeof(T), typePropertName)) { }
    }

    /// <summary>
    /// A factory class that creates instances of <see cref="JsonPolymorphicConverter{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the base class in the inheritance hierarchy.</typeparam>
    public class JsonPolymorphicConverterFactory<T> : JsonConverterFactory, IJsonPolymorphicConverterFactory where T : class, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPolymorphicConverterFactory{T}"/> class.
        /// </summary>
        /// <param name="typePropertyName">The name of the property in which to serialize the type name.</param>
        public JsonPolymorphicConverterFactory(string typePropertyName) {
            TypePropertyName = typePropertyName;
        }

        /// <inheritdoc />
        public string TypePropertyName { get; }
        /// <inheritdoc />
        public Type BaseType => typeof(T);

        /// <inheritdoc />
        public override bool CanConvert(Type typeToConvert) {
            var isDerivedType = typeof(T).IsAssignableFrom(typeToConvert);
            return isDerivedType;
        }

        /// <inheritdoc />
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) {
            var converterType = typeof(JsonPolymorphicConverter<>).MakeGenericType(typeToConvert);
            object converter;
            if (string.IsNullOrEmpty(TypePropertyName)) {
                converter = Activator.CreateInstance(converterType);
            } else {
                converter = Activator.CreateInstance(converterType, TypePropertyName);
            }
            return (JsonConverter)converter;
        }
    }

    /// <summary>
    /// Marker interface for unifying the generic <see cref="JsonPolymorphicConverterFactory{T}"/>.
    /// </summary>
    public interface IJsonPolymorphicConverterFactory
    {
        /// <summary>
        /// Gets the name of the property in which to serialize the type name or discriminator value.
        /// </summary>
        string TypePropertyName { get; }
        /// <summary>
        /// The type of the base class.
        /// </summary>
        Type BaseType { get; }
    }
}
