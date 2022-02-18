using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using Indice.Extensions;
using Indice.Serialization;
using Indice.Types;

namespace Indice.Services
{
    /// <inheritdoc/>
    public class EventDispatcherAzure : IEventDispatcher
    {
        /// <summary>
        /// The default name of the storage connection string.
        /// </summary>
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly bool _enabled;
        private readonly Func<ClaimsPrincipal> _claimsPrincipalSelector;
        private readonly Func<Guid?> _tenantIdSelector;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly string _environmentName;
        private readonly string _connectionString;

        /// <summary>
        /// Create a new <see cref="EventDispatcherAzure"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Storage account. By default it searches for <see cref="CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</param>
        /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
        /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
        /// <param name="claimsPrincipalSelector">Provides a way to access the current <see cref="ClaimsPrincipal"/> inside a service.</param>
        /// <param name="tenantIdSelector">Provides a way to access the current tenant id if any.</param>
        public EventDispatcherAzure(string connectionString, string environmentName, bool enabled, Func<ClaimsPrincipal> claimsPrincipalSelector, Func<Guid?> tenantIdSelector) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(environmentName)) {
                _environmentName = "production";
            }
            _enabled = enabled;
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _connectionString = connectionString;
            _claimsPrincipalSelector = claimsPrincipalSelector ?? throw new ArgumentNullException(nameof(claimsPrincipalSelector));
            _tenantIdSelector = tenantIdSelector ?? new Func<Guid?> (() => new Guid?());
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
            // Special cases string, byte[] or stream.
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
                ? new BinaryData(Envelope.Create(user, payload, _tenantIdSelector()), options: _jsonSerializerOptions, type: typeof(Envelope<TEvent>))
                : new BinaryData(payload, options: _jsonSerializerOptions, type: typeof(TEvent));
            await queue.SendMessageAsync(data, visibilityTimeout);
        }

        private async Task<QueueClient> EnsureExistsAsync(string queueName) {
            var queueClient = new QueueClient(_connectionString, queueName, new QueueClientOptions {
                MessageEncoding = QueueMessageEncoding.Base64
            });
            await queueClient.CreateIfNotExistsAsync();
            return queueClient;
        }
    }

    /// <summary>
    /// Options for configuring <see cref="EventDispatcherAzure"/>.
    /// </summary>
    public class EventDispatcherAzureOptions
    {
        /// <summary>
        /// The connection string to the Azure Storage account. By default it searches for <see cref="EventDispatcherAzure.CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.
        /// </summary>
        public string ConnectionString { get; set; }
        /// <summary>
        /// The environment name to use. Defaults to 'Production'.
        /// </summary>
        public string EnvironmentName { get; set; } = "Production";
        /// <summary>
        /// Provides a way to enable/disable event dispatching at will. Defaults to true.
        /// </summary>
        public bool Enabled { get; set; } = true;
        /// <summary>
        /// A function that retrieves the current thread user from the current operation context.
        /// </summary>
        public Func<ClaimsPrincipal> ClaimsPrincipalSelector { get; set; }
        /// <summary>
        /// A function that retrieves the current tenant id by any means possible. This is optional.
        /// </summary>
        public Func<Guid?> TenantIdSelector { get; set; }
    }
}
