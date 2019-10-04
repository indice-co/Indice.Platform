using System;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.Extensions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Indice.Services
{
    /// <inheritdoc/>
    internal class EventDispatcherAzure : IEventDispatcher
    {
        public const string CONNECTION_STRING_NAME = "StorageConnection";
        private readonly bool _enabled;
        private readonly CloudStorageAccount _storageAccount;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly string _environmentName;

        /// <summary>
        /// Create a new <see cref="EventDispatcherAzure"/> instance.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Storage account. By default it searches for <see cref="CONNECTION_STRING_NAME"/> application setting inside ConnectionStrings section.</param>
        /// <param name="environmentName">The environment name to use. Defaults to 'Production'.</param>
        /// <param name="enabled">Provides a way to enable/disable event dispatching at will. Defaults to true.</param>
        /// <param name="httpContextAccessor">Provides a way to access the current <see cref="HttpContext"/> inside a service.</param>
        public EventDispatcherAzure(string connectionString, string environmentName, bool enabled, IHttpContextAccessor httpContextAccessor) {
            if (string.IsNullOrEmpty(connectionString)) {
                throw new ArgumentNullException(nameof(connectionString));
            }
            if (string.IsNullOrEmpty(environmentName)) {
                _environmentName = "production";
            }
            _enabled = enabled;
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _jsonSerializerSettings = new JsonSerializerSettings {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                NullValueHandling = NullValueHandling.Ignore
            };
            _storageAccount = CloudStorageAccount.Parse(connectionString);
        }

        /// <inheritdoc/>
        public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? initialVisibilityDelay = null) where TEvent : class, new() {
            if (!_enabled) {
                return;
            }
            var queueName = $"{_environmentName}-{typeof(TEvent).Name.ToKebabCase()}";
            var queue = await EnsureExistsAsync(queueName);
            var user = actingPrincipal ?? _httpContextAccessor.HttpContext.User;
            // Create a message and add it to the queue.
            var serializedMessage = JsonConvert.SerializeObject(Envelope.Create(user, payload), _jsonSerializerSettings);
            var envelope = new CloudQueueMessage(serializedMessage);
            await queue.AddMessageAsync(envelope, null, initialVisibilityDelay, null, null);
        }

        private async Task<CloudQueue> EnsureExistsAsync(string queueName) {
            var queueClient = _storageAccount.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync();
            return queue;
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
