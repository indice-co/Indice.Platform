using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.Extensions;
using Indice.Types;
using Azure.Storage.Queues;
using System.Text.Json;
using Indice.Serialization;

namespace Indice.Services
{
    /// <inheritdoc/>
    public class EventDispatcherAzure : IEventDispatcher
    {
        /// <summary>
        /// The name of the storage connection string
        /// </summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly bool _enabled;
        private readonly Func<ClaimsPrincipal> _getUserFunc;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly string _environmentName;
        private readonly string _connectionString;

        /// <summary>
        /// Create a new <see cref="EventDispatcherAzure"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Storage account. By default it searches for <see cref="CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</param>
        /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
        /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
        /// <param name="getUserFunc">Provides a way to access the current <see cref="ClaimsPrincipal"/> inside a service.</param>
        public EventDispatcherAzure(string connectionString, string environmentName, bool enabled, Func<ClaimsPrincipal> getUserFunc) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(environmentName)) {
                _environmentName = "production";
            }
            _enabled = enabled;
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _connectionString = connectionString;
            _getUserFunc = getUserFunc ?? throw new ArgumentNullException(nameof(getUserFunc));
            _jsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        }

        /// <inheritdoc/>
        public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityDelay = null) where TEvent : class, new() {
            if (!_enabled) {
                return;
            }
            var queueName = $"{_environmentName}-{typeof(TEvent).Name.ToKebabCase()}";
            var queue = await EnsureExistsAsync(queueName);
            var user = actingPrincipal ?? _getUserFunc?.Invoke();
            // Create a message and add it to the queue.
            var serializedMessage = JsonSerializer.Serialize(Envelope.Create(user, payload), _jsonSerializerOptions);
            await queue.SendMessageAsync(serializedMessage, visibilityTimeout: visibilityDelay);
        }

        private async Task<QueueClient> EnsureExistsAsync(string queueName) {
            var queueClient = new QueueClient(_connectionString, queueName);
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }
    }

    /// <summary>
    /// Options for configuring <see cref="EventDispatcherAzure"/>.
    /// </summary>
    public class EventDispatcherOptions
    {
        /// <summary>
        /// The connection string to the Azure Storage account. By default it searches for <see cref="EventDispatcherAzure.CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// The environment name to use. Defaults to 'Production'.
        /// </summary>
        public string EnvironmentName { get; set; }
        /// <summary>
        /// Provides a way to enable/disable event dispatching at will. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; }
    }


}
