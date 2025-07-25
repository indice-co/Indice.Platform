using Newtonsoft.Json;

namespace Indice.Features.Cases.Workflows.Serialization;

/// <inheritdoc />
public class JsonElementToJsonObjectAdapterConverter : JsonConverter<System.Text.Json.JsonElement>
{
    /// <inheritdoc />
    public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer) {
        return System.Text.Json.JsonDocument.Parse(reader.Value?.ToString() ?? string.Empty).RootElement;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer) {
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
    }

}