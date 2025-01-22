using Newtonsoft.Json;

namespace Indice.Features.Cases.Core.Serialization;
/// <summary>
/// A custom JSON converter that adapts System.Text.Json.JsonElement to be used with Newtonsoft.Json.
/// </summary>
public class JsonElementToJsonObjectAdapterConverter : JsonConverter<System.Text.Json.JsonElement>
{
    /// <inheritdoc />
    public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return System.Text.Json.JsonDocument.Parse(reader.Value?.ToString() ?? string.Empty).RootElement;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer)
    {
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
    }
}
