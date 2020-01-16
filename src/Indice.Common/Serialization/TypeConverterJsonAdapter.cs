using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// Adapter between <see cref="TypeConverter"/> and <see cref="JsonConverter"/>.
    /// </summary>
    public class TypeConverterJsonAdapter : JsonConverter<object>
    {
        /// <inheritdoc/>
        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            var converter = TypeDescriptor.GetConverter(typeToConvert);
            var text = reader.GetString();
            return converter.ConvertFromString(text);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, object objectToWrite, JsonSerializerOptions options) {
            var converter = TypeDescriptor.GetConverter(objectToWrite);
            var text = converter.ConvertToString(objectToWrite);
            writer.WriteStringValue(text);
        }

        /// <inheritdoc/>
        public override bool CanConvert(Type typeToConvert) {
            var hasConverter = typeToConvert.GetCustomAttributes<TypeConverterAttribute>(inherit: true).Any();
            if (!hasConverter) {
                return false;
            }
            return true;
        }
    }
}
