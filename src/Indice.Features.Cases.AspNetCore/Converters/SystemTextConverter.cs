using System.Text.Json;

namespace Indice.Features.Cases.Converters;

/// <summary>
/// Properly serialize dynamic CaseData from SystemText to Newtonsoft.
/// CaseData is using SystemText serializer to save/retrieve data from storage.
/// <remarks>Elsa package forcing the API to use newtonsoft</remarks>
/// </summary>
internal class SystemTextConverter : Newtonsoft.Json.JsonConverter<JsonElement>
{
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, JsonElement value, Newtonsoft.Json.JsonSerializer serializer) {
        writer.WriteRawValue(JsonSerializer.Serialize(value));
    }

    public override JsonElement ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, JsonElement existingValue, bool hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
        => throw new NotImplementedException();
}