using System.Security.Claims;
using Indice.Services;

namespace Indice.Hosting.Services;

/// <summary>An implementation of <see cref="IEventDispatcher"/> that creates queue items in the <see cref="IMessageQueue{T}"/> that can be consumed by background services.</summary>
public class EventDispatcherHosting : IEventDispatcher
{
    private readonly MessageQueueFactory _messageQueueFactory;

    /// <summary>Creates a new instance of <see cref="EventDispatcherHosting"/>.</summary>
    /// <param name="messageQueueFactory">Provides instances of <see cref="IMessageQueue{T}"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="messageQueueFactory"/> is null.</exception>
    public EventDispatcherHosting(MessageQueueFactory messageQueueFactory) {
        _messageQueueFactory = messageQueueFactory ?? throw new ArgumentNullException(nameof(messageQueueFactory));
    }

    /// <inheritdoc />
    public async Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal? actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string? queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class {
        var messageQueue = _messageQueueFactory.Create<TEvent>();
        await messageQueue.Enqueue(payload, visibilityTimeout ?? TimeSpan.Zero);
    }
}
