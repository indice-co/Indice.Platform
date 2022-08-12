using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Indice.Extensions;
using Indice.Serialization;
using Indice.Types;
using Microsoft.Extensions.Hosting;

namespace Indice.Services
{
    /// <inheritdoc/>
    public class EventDispatcherAzure : IEventDispatcher
    {
        /// <summary>The default name of the storage connection string.</summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly string _connectionString;
        private readonly string _environmentName;
        private readonly bool _enabled;
        private readonly bool _useCompression;
        private readonly QueueMessageEncoding _queueMessageEncoding;
        private readonly Func<ClaimsPrincipal> _claimsPrincipalSelector;
        private readonly Func<Guid?> _tenantIdSelector;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>Create a new <see cref="EventDispatcherAzure"/> instance.</summary>
        /// <param name="connectionString">The connection string to the Azure Storage account. By default it searches for <see cref="CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</param>
        /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
        /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
        /// <param name="useCompression">When selected, applies Brotli compression algorithm in the queue message payload. Defaults to false.</param>
        /// <param name="queueMessageEncoding">Determines how <see cref="Azure.Storage.Queues.Models.QueueMessage.Body"/> is represented in HTTP requests and responses.</param>
        /// <param name="claimsPrincipalSelector">Provides a way to access the current <see cref="ClaimsPrincipal"/> inside a service.</param>
        /// <param name="tenantIdSelector">Provides a way to access the current tenant id if any.</param>
        public EventDispatcherAzure(string connectionString, string environmentName, bool enabled, bool useCompression, QueueMessageEncoding queueMessageEncoding, Func<ClaimsPrincipal> claimsPrincipalSelector, Func<Guid?> tenantIdSelector) {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _enabled = enabled;
            _useCompression = useCompression;
            _queueMessageEncoding = queueMessageEncoding;
            _claimsPrincipalSelector = claimsPrincipalSelector ?? throw new ArgumentNullException(nameof(claimsPrincipalSelector));
            _tenantIdSelector = tenantIdSelector ?? new Func<Guid?>(() => new Guid?());
            _jsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        }

        /// <inheritdoc/>
        public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class {
            if (!_enabled) {
                return;
            }
            if (string.IsNullOrWhiteSpace(queueName)) {
                queueName = typeof(TEvent).Name.ToKebabCase();
            }
            if (prependEnvironmentInQueueName) {
                queueName = $"{_environmentName}-{queueName}";
            }
            var queue = await EnsureExistsAsync(queueName);
            var user = actingPrincipal ?? _claimsPrincipalSelector?.Invoke();
            var payloadBytes = Array.Empty<byte>();
            // Special cases string, byte[] or stream.
            switch (payload) {
                case string text: payloadBytes = Encoding.UTF8.GetBytes(text); break;
                case byte[] bytes: payloadBytes = bytes; break;
                case ReadOnlyMemory<byte> memory: payloadBytes = memory.ToArray(); break;
                case Stream stream:
                    await using (var memoryStream = new MemoryStream()) {
                        await stream.CopyToAsync(memoryStream);
                        payloadBytes = memoryStream.ToArray();
                    }
                    break;
                default:
                    // Create a message and add it to the queue.
                    var jsonPayload = wrap
                        ? JsonSerializer.Serialize(Envelope.Create(user, payload, _tenantIdSelector()), _jsonSerializerOptions)
                        : JsonSerializer.Serialize(payload, _jsonSerializerOptions);
                    payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);
                    break;
            }
            var maxTimeSpan = TimeSpan.FromDays(5);
            visibilityTimeout = visibilityTimeout.HasValue && visibilityTimeout.Value > maxTimeSpan ? maxTimeSpan : visibilityTimeout;
            if (_useCompression) {
                await queue.SendMessageAsync(new BinaryData(await CompressionUtils.Compress(payloadBytes)), visibilityTimeout);
                return;
            }
            await queue.SendMessageAsync(new BinaryData(payloadBytes), visibilityTimeout);
        }

        private async Task<QueueClient> EnsureExistsAsync(string queueName) {
            var queueClient = new QueueClient(_connectionString, queueName, new QueueClientOptions {
                MessageEncoding = _queueMessageEncoding
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }
    }

    /// <summary>Options for configuring <see cref="EventDispatcherAzure"/>.</summary>
    public class EventDispatcherAzureOptions
    {
        /// <summary>The connection string to the Azure Storage account. By default it searches for <see cref="EventDispatcherAzure.CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</summary>
        public string ConnectionString { get; set; }
        /// <summary>The environment name to use. Defaults to <see cref="IHostEnvironment.EnvironmentName"/>.</summary>
        public string EnvironmentName { get; set; } = "Production";
        /// <summary>Provides a way to enable/disable event dispatching at will. Defaults to true.</summary>
        public bool Enabled { get; set; } = true;
        /// <summary>A function that retrieves the current thread user from the current operation context.</summary>
        public Func<ClaimsPrincipal> ClaimsPrincipalSelector { get; set; }
        /// <summary>A function that retrieves the current tenant id by any means possible. This is optional.</summary>
        public Func<Guid?> TenantIdSelector { get; set; }
        /// <summary>Determines how <see cref="Azure.Storage.Queues.Models.QueueMessage.Body"/> is represented in HTTP requests and responses.</summary>
        public QueueMessageEncoding QueueMessageEncoding { get; set; } = QueueMessageEncoding.Base64;
        /// <summary>When selected, applies Brotli compression algorithm in the queue message payload. Defaults to false.</summary>
        public bool UseCompression { get; set; }
    }
}
