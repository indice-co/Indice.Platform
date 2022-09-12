using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Indice.Services
{
    /// <summary>Provides methods that allow application components to communicate with each other by dispatching events.</summary>
    public interface IEventDispatcher
    {
        /// <summary>Dispatches an event of the specified type.</summary>
        /// <typeparam name="TEvent">The concrete type of the payload to send.</typeparam>
        /// <param name="payload">The actual payload data to send.</param>
        /// <param name="actingPrincipal">A <see cref="ClaimsPrincipal"/> instance that contains information about the entity that triggered the event.</param>
        /// <param name="visibilityTimeout">Delays the sending of payload to the queue for the specified amount of time. The maximum delay can reach up to 7 days.</param>
        /// <param name="wrap">Wrap around an envelope object. Defaults to true.</param>
        /// <param name="queueName">The name of the queue. If not specified, the name of <typeparamref name="TEvent"/> in kebab case is used.</param>
        /// <param name="prependEnvironmentInQueueName">When set to true, it prepends the queue name with the environment name. For example <b>production-my-queue-name</b>. Defaults to true.</param>
        Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class;
    }

    /// <summary>Extension methods on <see cref="IEventDispatcher"/>.</summary>
    public static class IEventDispatcherExtensions
    {
        /// <summary>Dispatches an event of the specified type.</summary>
        /// <typeparam name="TEvent">The concrete type of the payload to send.</typeparam>
        /// <param name="eventDispatcher">Provides methods that allow application components to communicate with each other by dispatching events.</param>
        /// <param name="payload">The actual payload data to send.</param>
        /// <param name="configure">Configuration action for <see cref="EventDispatcherRaiseOptions"/>.</param>
        public static Task RaiseEventAsync<TEvent>(this IEventDispatcher eventDispatcher, TEvent payload, Action<EventDispatcherRaiseOptionsBuilder> configure = null) where TEvent : class {
            var optionsBuilder = new EventDispatcherRaiseOptionsBuilder();
            configure?.Invoke(optionsBuilder);
            var options = optionsBuilder.Build();
            return eventDispatcher.RaiseEventAsync(payload, options.ClaimsPrincipal, options.VisibilityTimeout, options.Wrap, options.QueueName, options.PrependEnvironmentInQueueName);
        }
    }

    /// <summary>
    /// Options for configuring <see cref="IEventDispatcher.RaiseEventAsync{TEvent}(TEvent, ClaimsPrincipal, TimeSpan?, bool, string, bool)"/> method.
    /// </summary>
    public class EventDispatcherRaiseOptions
    {
        /// <summary>A <see cref="System.Security.Claims.ClaimsPrincipal"/> instance that contains information about the entity that triggered the event.</summary>
        public ClaimsPrincipal ClaimsPrincipal { get; set; }
        /// <summary>Delays the sending of payload to the queue for the specified amount of time. The maximum delay can reach up to 7 days.</summary>
        public TimeSpan? VisibilityTimeout { get; set; }
        /// <summary>Wrap around an envelope object. Defaults to true.</summary>
        public bool Wrap { get; set; } = true;
        /// <summary>The name of the queue. If not specified, the name of event in kebab case is used.</summary>
        public string QueueName { get; set; }
        /// <summary>When set to true, it prepends the queue name with the environment name. For example <b>production-my-queue-name</b>. Defaults to true.</summary>
        public bool PrependEnvironmentInQueueName { get; set; } = true;
    }

    /// <summary>An abstraction for implementing a builder for <see cref="EventDispatcherRaiseOptions"/>.</summary>
    public interface IEventDispatcherRaiseOptionsBuilder
    {
        /// <summary>Defines a <see cref="ClaimsPrincipal"/> instance that contains information about the entity that triggered the event.</summary>
        /// <param name="claimsPrincipal">The principal.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder UsingPrincipal(ClaimsPrincipal claimsPrincipal);
        /// <summary>Defines a delay when sending the payload to the queue. The maximum delay can reach up to 7 days.</summary>
        /// <param name="delay">The delay <see cref="TimeSpan"/>.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder Delay(TimeSpan delay);
        /// <summary>Defines a delay when sending the payload to the queue.</summary>
        /// <param name="dateTime">The <see cref="DateTime"/> value that the event is dispatched.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder At(DateTime dateTime);
        /// <summary>Defines whether to wrap payload around an envelope object or not. Defaults to true.</summary>
        /// <param name="wrap">Wrap.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder WrapInEnvelope(bool wrap = true);
        /// <summary>Defines the name of the queue. If not specified, the name of event in kebab case is used.</summary>
        /// <param name="queueName">The queue name.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder WithQueueName(string queueName);
        /// <summary>Defines whether prepends the queue name with the environment name or not. For example <b>production-my-queue-name</b>. Defaults to true.</summary>
        /// <param name="prepend">Prepend.</param>
        /// <returns>The builder to construct the <see cref="EventDispatcherRaiseOptions"/> instance.</returns>
        IEventDispatcherRaiseOptionsBuilder PrependEnvironmentInQueueName(bool prepend = true);
        /// <summary>Creates the actual instance of <see cref="EventDispatcherRaiseOptions"/>.</summary>
        EventDispatcherRaiseOptions Build();
    }

    /// <summary>An implementation for <see cref="IEventDispatcherRaiseOptionsBuilder"/>.</summary>
    public class EventDispatcherRaiseOptionsBuilder : IEventDispatcherRaiseOptionsBuilder
    {
        /// <summary>The <see cref="EventDispatcherRaiseOptions"/> instance that the builder creates.</summary>
        protected EventDispatcherRaiseOptions Options = new();

        /// <inheritdoc />
        public EventDispatcherRaiseOptions Build() => Options;

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder UsingPrincipal(ClaimsPrincipal claimsPrincipal) {
            Options.ClaimsPrincipal = claimsPrincipal;
            return this;
        }

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder Delay(TimeSpan delay) {
            Options.VisibilityTimeout = delay;
            return this;
        }

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder WrapInEnvelope(bool wrap = true) {
            Options.Wrap = wrap;
            return this;
        }

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder WithQueueName(string queueName) {
            Options.QueueName = queueName;
            return this;
        }

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder PrependEnvironmentInQueueName(bool prepend = true) {
            Options.PrependEnvironmentInQueueName = prepend;
            return this;
        }

        /// <inheritdoc />
        public IEventDispatcherRaiseOptionsBuilder At(DateTime dateTime) {
            var visibilityTimeout = dateTime - DateTime.UtcNow;
            Options.VisibilityTimeout = visibilityTimeout <= TimeSpan.Zero ? null : visibilityTimeout;
            return this;
        }
    }
}
