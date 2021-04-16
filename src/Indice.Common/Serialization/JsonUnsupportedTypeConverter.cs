using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// This converter is a NO Op conveter for the type <typeparamref name="TIgnore"/>
    /// </summary>
    /// <typeparam name="TIgnore">The type to ignore when serializing</typeparam>
    public class JsonUnsupportedTypeConverter<TIgnore> : JsonConverter<TIgnore>
    {
        /// <inheritdoc/>
        public override TIgnore Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
            return default;
        }


        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TIgnore value, JsonSerializerOptions options) {
            writer.WriteNullValue();
        }
    }
}
