using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Indice.Extensions;
using Indice.Serialization;
using Indice.Types;

namespace Indice.Services
{
    /// <inheritdoc/>
    public class EventDispatcherAzure : IEventDispatcher
    {
        /// <summary>
        /// The name of the storage connection string.
        /// </summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly bool _enabled;
        private readonly QueueMessageEncoding _messageEncoding;
        private readonly Func<ClaimsPrincipal> _claimsPrincipalSelector;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly string _environmentName;
        private readonly string _connectionString;

        /// <summary>
        /// Create a new <see cref="EventDispatcherAzure"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Storage account. By default it searches for <see cref="CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</param>
        /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
        /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
        /// <param name="messageEncoding">Queue message encoding.</param>
        /// <param name="claimsPrincipalSelector">Provides a way to access the current <see cref="ClaimsPrincipal"/> inside a service.</param>
        public EventDispatcherAzure(string connectionString, string environmentName, bool enabled, QueueMessageEncoding messageEncoding, Func<ClaimsPrincipal> claimsPrincipalSelector) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(environmentName)) {
                _environmentName = "production";
            }
            _enabled = enabled;
            _messageEncoding = messageEncoding;
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _connectionString = connectionString;
            _claimsPrincipalSelector = claimsPrincipalSelector ?? throw new ArgumentNullException(nameof(claimsPrincipalSelector));
            _jsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        }

        /// <inheritdoc/>
        public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string queueName = null) where TEvent : class {
            if (!_enabled) {
                return;
            }
            queueName = $"{_environmentName}-{queueName?.ToLowerInvariant() ?? typeof(TEvent).Name.ToKebabCase()}";
            var queue = await EnsureExistsAsync(queueName);
            var user = actingPrincipal ?? _claimsPrincipalSelector?.Invoke();

            // special cases string, byte[] or stream
            switch (payload) {
                case string text: await queue.SendMessageAsync(text, visibilityTimeout); return;
                case byte[] bytes: await queue.SendMessageAsync(new BinaryData(bytes), visibilityTimeout); return;
                case ReadOnlyMemory<byte> memory: await queue.SendMessageAsync(new BinaryData(memory), visibilityTimeout); return;
                case Stream stream:
                    using (var memoryStream = new MemoryStream()) {
                        stream.CopyTo(memoryStream);
                        await queue.SendMessageAsync(new BinaryData(memoryStream.ToArray()), visibilityTimeout);
                    }
                    return;
            }
            // Create a message and add it to the queue.
            var data = wrap
                ? new BinaryData(Envelope.Create(user, payload), options: _jsonSerializerOptions, type: typeof(Envelope<TEvent>))
                : new BinaryData(payload, options: _jsonSerializerOptions, type: typeof(TEvent));
            await queue.SendMessageAsync(data, visibilityTimeout);
        }

        private async Task<QueueClient> EnsureExistsAsync(string queueName) {
            var queueClient = new QueueClient(_connectionString, queueName, new QueueClientOptions {
                MessageEncoding = _messageEncoding
            });
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
        /// <summary>
        /// Determines how <see cref="QueueMessage.Body"/> is represented in HTTP requests and responses.
        /// </summary>
        public QueueMessageEncoding MessageEncoding { get; set; }
        /// <summary>
        /// A function that retrieves the current thread user from the current operation context.
        /// </summary>
        public Func<ClaimsPrincipal> ClaimsPrincipalSelector { get; set; }
    }
}
