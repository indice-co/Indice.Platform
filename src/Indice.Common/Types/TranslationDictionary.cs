using System.Text.Json;
using Indice.Serialization;

namespace Indice.Types;

/// <summary>A type that models the translation of an object.</summary>
/// <typeparam name="T">The type of object to translate.</typeparam>
public class TranslationDictionary<T> : Dictionary<string, T> where T : notnull
{
    /// <summary>Creates a new instance of <see cref="TranslationDictionary{T}"/>.</summary>
    /// <param name="source"></param>
    public TranslationDictionary(IDictionary<string, T> source) : this() {
        foreach (var item in source) {
            Add(item.Key.ToLower(), item.Value);
        }
    }

    /// <summary>Creates a new instance of <see cref="TranslationDictionary{T}"/>.</summary>
    public TranslationDictionary() : base(StringComparer.InvariantCultureIgnoreCase) { }

    /// <summary>Transforms the current <see cref="TranslationDictionary{T}"/> into an <see cref="IEnumerable{Translation}"/></summary>
    public IEnumerable<Translation<T>> ToTranslationEnumerable() {
        foreach (var item in this) {
            yield return new Translation<T>(item.Key, item.Value);
        }
    }

    /// <summary>Converts the current instance of <see cref="TranslationDictionary{T}"/> to it's JSON representation.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, JsonSerializerOptionDefaults.GetDefaultSettings());

    /// <summary>Creates a <see cref="TranslationDictionary{T}"/> from it's JSON representation.</summary>
    /// <param name="json">The JSON to create the <see cref="TranslationDictionary{T}"/>.</param>
    public static TranslationDictionary<T>? FromJson(string json) {
        if (string.IsNullOrWhiteSpace(json)) {
            return default;
        }
        return JsonSerializer.Deserialize<TranslationDictionary<T>>(json, JsonSerializerOptionDefaults.GetDefaultSettings());
    }
}
