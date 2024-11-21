using System.Security.Claims;

namespace Indice.Services;

/// <summary>A <see cref="IEventDispatcher"/> implementation that does nothing.</summary>
public class EventDispatcherNoop : IEventDispatcher
{
    /// <inheritdoc/>
    public Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal? actingPrincipal = null, TimeSpan? visibilityTimeout = null, bool wrap = true, string? queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class => Task.CompletedTask;
}
