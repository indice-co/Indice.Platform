using System;
using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Indice.Extensions;
using Indice.Hosting.Tasks;
using Indice.Serialization;
using Indice.Types;

namespace Indice.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class EventDispatcherMessageQueue : IEventDispatcher
    {
        private readonly MessageQueueFactory _messageQueueFactory;
        private readonly string _environmentName;
        private readonly bool _enabled;
        private readonly Func<ClaimsPrincipal> _claimsPrincipalSelector;
        private readonly Func<Guid?> _tenantIdSelector;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="messageQueueFactory"></param>
        /// <param name="environmentName"></param>
        /// <param name="enabled"></param>
        /// <param name="claimsPrincipalSelector"></param>
        /// <param name="tenantIdSelector"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public EventDispatcherMessageQueue(MessageQueueFactory messageQueueFactory, string environmentName, bool enabled, Func<ClaimsPrincipal> claimsPrincipalSelector, Func<Guid?> tenantIdSelector) {
            _messageQueueFactory = messageQueueFactory ?? throw new ArgumentNullException(nameof(messageQueueFactory));
            _environmentName = Regex.Replace(environmentName ?? "Development", @"\s+", "-").ToLowerInvariant();
            _enabled = enabled;
            _claimsPrincipalSelector = claimsPrincipalSelector ?? throw new ArgumentNullException(nameof(claimsPrincipalSelector));
            _tenantIdSelector = tenantIdSelector ?? new Func<Guid?>(() => new Guid?());
            _jsonSerializerOptions = JsonSerializerOptionDefaults.GetDefaultSettings();
        }

        /// <inheritdoc />
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
            var user = actingPrincipal ?? _claimsPrincipalSelector?.Invoke();
            if (wrap) {
                var messageQueue = _messageQueueFactory.Create<Envelope<TEvent>>();
                await messageQueue.Enqueue(Envelope.Create(user, payload, _tenantIdSelector()), visibilityTimeout.Value);
            } else {
                var messageQueue = _messageQueueFactory.Create<TEvent>();
                await messageQueue.Enqueue(payload, visibilityTimeout.Value);
            }
        }
    }
}
