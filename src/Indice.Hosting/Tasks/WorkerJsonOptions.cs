using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Hosting
{
    /// <summary>
    /// These are the options regarding Json Serialization. They are used internally for persisting payloads.
    /// </summary>
    public class WorkerJsonOptions
    {
        /// <summary>
        /// Serializer options
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; set; } = GetDefaultSettings();

        /// <summary>
        /// Json options Defaults.
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerOptions GetDefaultSettings() {
            var options = new JsonSerializerOptions() {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new Serialization.TypeConverterJsonAdapterFactory());
            options.Converters.Add(new Serialization.JsonObjectToInferredTypeConverter());
            return options;
        }
    }
}
