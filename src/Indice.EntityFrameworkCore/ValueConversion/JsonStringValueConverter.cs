using System.Text.Json;
using Indice.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.EntityFrameworkCore.ValueConversion
{
    /// <summary>
    /// Value converter for EF core. Takes care of conversion between a string coming from a JSON field in the database and the concrete CLR type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonStringValueConverter<T> : ValueConverter<T, string> where T : class
    {
        /// <summary>
        /// Serialization options for the <see cref="JsonStringValueConverter{T}"/>. 
        /// </summary>
        public static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

        /// <inheritdoc/>
        public JsonStringValueConverter() : base(
            x => x != null ? JsonSerializer.Serialize(x, SerializerOptions) : null,
            x => x != null ? JsonSerializer.Deserialize<T>(x, SerializerOptions) : null
        ) { }
    }
}
