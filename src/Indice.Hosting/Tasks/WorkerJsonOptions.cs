using System.Text.Json;
using Indice.Serialization;

namespace Indice.Hosting.Tasks
{
    /// <summary>
    /// These are the options regarding json Serialization. They are used internally for persisting payloads.
    /// </summary>
    public class WorkerJsonOptions
    {
        /// <summary>
        /// Serializer options.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = GetDefaultSettings();

        /// <summary>
        /// Json options defaults.
        /// </summary>
        public static JsonSerializerOptions GetDefaultSettings() => JsonSerializerOptionDefaults.GetDefaultSettings();
    }
}
