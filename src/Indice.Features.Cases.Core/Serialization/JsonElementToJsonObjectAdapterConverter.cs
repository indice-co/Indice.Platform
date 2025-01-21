using Newtonsoft.Json;

namespace Indice.Features.Cases.Core.Serialization;
/// <summary>
/// A custom JSON converter that adapts System.Text.Json.JsonElement to be used with Newtonsoft.Json.
/// </summary>
public class JsonElementToJsonObjectAdapterConverter : JsonConverter<System.Text.Json.JsonElement>
{
    /// <summary>
    /// Reads JSON and converts it to a System.Text.Json.JsonElement.
    /// </summary>
    /// <param name="reader">The Newtonsoft.Json.JsonReader to read from.</param>
    /// <param name="objectType">The type of the object to convert.</param>
    /// <param name="existingValue">The existing value of the object being read.</param>
    /// <param name="hasExistingValue">A flag indicating whether there is an existing value.</param>
    /// <param name="serializer">The calling serializer.</param>
    /// <returns>A System.Text.Json.JsonElement representing the JSON data.</returns>
    public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return System.Text.Json.JsonDocument.Parse(reader.Value?.ToString() ?? string.Empty).RootElement;
    }

    /// <summary>
    /// Writes a System.Text.Json.JsonElement to JSON.
    /// </summary>
    /// <param name="writer">The Newtonsoft.Json.JsonWriter to write to.</param>
    /// <param name="value">The System.Text.Json.JsonElement to write.</param>
    /// <param name="serializer">The calling serializer.</param>
    public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer)
    {
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
    }
}
