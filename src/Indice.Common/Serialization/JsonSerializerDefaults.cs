using System.Text.Json;
using System.Text.Json.Serialization;

namespace Indice.Serialization
{
    /// <summary>
    /// Carries default JSON serializer settings for the most common scenarios. 
    /// </summary>
    public static class JsonSerializerOptionDefaults
    {
        /// <summary>
        /// JSON options defaults.
        /// </summary>
        public static JsonSerializerOptions GetDefaultSettings() {
            var options = new JsonSerializerOptions {
#if NET5_0_OR_GREATER
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
#else          
                IgnoreNullValues = true,
#endif
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new JsonStringEnumConverter());
            options.Converters.Add(new JsonTimeSpanConverter());
            options.Converters.Add(new JsonNullableTimeSpanConverter());
            options.Converters.Add(new TypeConverterJsonAdapterFactory());
            options.Converters.Add(new ValueTupleJsonConverterFactory());
            options.Converters.Add(new JsonObjectToInferredTypeConverter());
            return options;
        }
    }
}
