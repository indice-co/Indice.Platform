using System.Collections.Generic;
using System.Text.Json;
using Indice.Serialization;

namespace Indice.Types
{
    /// <summary>
    /// A type that models the translation of an object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TranslationDictionary<T> : Dictionary<string, T> where T : class
    {
        /// <summary>
        /// Converts the current instance of <see cref="TranslationDictionary{T}"/> to it's JSON representation.
        /// </summary>
        public string ToJson() {
            if (this != null) {
                return JsonSerializer.Serialize(this, JsonSerializerOptionDefaults.GetDefaultSettings());
            }
            return default;
        }

        /// <summary>
        /// Creates a <see cref="TranslationDictionary{T}"/> from it's JSON representation.
        /// </summary>
        /// <param name="json"></param>
        public static TranslationDictionary<T> FromJson(string json) {
            if (string.IsNullOrWhiteSpace(json)) {
                return default;
            }
            return JsonSerializer.Deserialize<TranslationDictionary<T>>(json, JsonSerializerOptionDefaults.GetDefaultSettings());
        }
    }
}
