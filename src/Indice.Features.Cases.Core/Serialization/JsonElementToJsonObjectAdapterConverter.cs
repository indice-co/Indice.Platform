using Newtonsoft.Json;

namespace Indice.Features.Cases.Core.Serialization;
public class JsonElementToJsonObjectAdapterConverter : JsonConverter<System.Text.Json.JsonElement>
{

    public override System.Text.Json.JsonElement ReadJson(JsonReader reader, Type objectType, System.Text.Json.JsonElement existingValue, bool hasExistingValue, JsonSerializer serializer) {
        return System.Text.Json.JsonDocument.Parse(reader.Value?.ToString() ?? string.Empty).RootElement;
    }

    public override void WriteJson(JsonWriter writer, System.Text.Json.JsonElement value, JsonSerializer serializer) {
        writer.WriteRawValue(System.Text.Json.JsonSerializer.Serialize(value));
    }

}