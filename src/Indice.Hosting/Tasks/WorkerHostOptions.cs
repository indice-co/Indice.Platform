using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Hosting
{
    /// <summary>
    /// Options for configuring the worker host.
    /// </summary>
    public class WorkerHostOptions
    {
        /// <summary>
        /// Creates a new instance of <see cref="WorkerHostOptions"/>.
        /// </summary>
        /// <param name="services">Specifies the contract for a collection of service descriptors.</param>
        public WorkerHostOptions(IServiceCollection services) : this(services, null, null) { }
        internal WorkerHostOptions(IServiceCollection services, Type workItemQueueType, JsonSerializerOptions jsonSerializerOptions) {
            Services = services;
            WorkItemQueueType = workItemQueueType;
            JsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions() {
                IgnoreNullValues = true,
                PropertyNameCaseInsensitive = true,
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            JsonSerializerOptions.Converters.Add(new Indice.Serialization.JsonStringDecimalConverter());
            JsonSerializerOptions.Converters.Add(new Indice.Serialization.JsonStringDoubleConverter());
            JsonSerializerOptions.Converters.Add(new Indice.Serialization.JsonStringInt32Converter());
            JsonSerializerOptions.Converters.Add(new Indice.Serialization.JsonObjectToInferredTypeConverter());
            JsonSerializerOptions.Converters.Add(new Indice.Serialization.TypeConverterJsonAdapter());
        }

        internal IServiceCollection Services { get; }
        internal Type WorkItemQueueType { get; set; }

        /// <summary>
        /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> used internally whenever a payload needs to be persisted. 
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; }
    }
}
