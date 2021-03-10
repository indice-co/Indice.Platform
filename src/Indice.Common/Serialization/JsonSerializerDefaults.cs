using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// Carries Default Json serializer Settings for the most common scenarios. 
    /// </summary>
    public static class JsonSerializerOptionDefaults

    {
        /// <summary>
        /// Json options defaults.
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerOptions GetDefaultSettings() {
            var options = new JsonSerializerOptions {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new TypeConverterJsonAdapterFactory());
            options.Converters.Add(new JsonObjectToInferredTypeConverter());
            options.Converters.Add(new ValueTupleJsonConverterFactory());
            return options;
        }
    }
}
