using System.Text.Json;
using Indice.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Indice.EntityFrameworkCore.ValueConversion
{
    /// <summary>Value converter for EF core. Takes care of conversion between a string coming from a JSON field in the database and the concrete CLR type.</summary>
    /// <typeparam name="T">The type to convert.</typeparam>
    public class JsonStringValueConverter<T> : ValueConverter<T, string> where T : class
    {
        /// <summary>Serialization options for the <see cref="JsonStringValueConverter{T}"/>. </summary>
        public static readonly JsonSerializerOptions SerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();

        /// <inheritdoc/>
        public JsonStringValueConverter() : base(
            type => type != null ? JsonSerializer.Serialize(type, SerializerOptions) : null,
            json => !string.IsNullOrWhiteSpace(json) ? JsonSerializer.Deserialize<T>(json, SerializerOptions) : null
        ) { }
    }
}
