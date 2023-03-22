using System.Text.Json;
using Indice.Serialization;

namespace Indice.Hosting;

/// <summary>These are the options regarding JSON serialization. They are used internally for persisting payloads.</summary>
public class WorkerJsonOptions
{
    /// <summary>Serializer options.</summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = GetDefaultSettings();

    /// <summary>JSON options defaults.</summary>
    public static JsonSerializerOptions GetDefaultSettings() => JsonSerializerOptionDefaults.GetDefaultSettings();
}
