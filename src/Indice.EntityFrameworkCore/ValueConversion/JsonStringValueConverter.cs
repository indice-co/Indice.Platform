using System.Text.Json;
using Indice.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.EntityFrameworkCore.ValueConversion
{
    /// <summary>
    /// Value converter for EF core. Takes care of convertion between a string comming from a JsonField in the database and the concrete CLR type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonStringValueConverter<T> : ValueConverter<T, string> where T : class
    {
        private static readonly JsonSerializerOptions _serializerOptions;

        /// <inheritdoc/>
        static JsonStringValueConverter() {
            _serializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        }

        /// <inheritdoc/>
        public JsonStringValueConverter() : base(
            x => x != null ? JsonSerializer.Serialize(x, _serializerOptions) : null,
            x => x != null ? JsonSerializer.Deserialize<T>(x, _serializerOptions) : null
        ) { }
    }
}
