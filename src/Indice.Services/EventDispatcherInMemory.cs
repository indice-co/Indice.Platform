﻿using System.Security.Claims;

namespace Indice.Services;

/// <inheritdoc/>
public class EventDispatcherInMemory : IEventDispatcher
{
    private readonly Queue<object> _queue = new();

    /// <inheritdoc/>
    public Task RaiseEventAsync<TEvent>(TEvent payload, ClaimsPrincipal? actingPrincipal = null, TimeSpan? initialVisibilityDelay = null, bool wrap = true, string? queueName = null, bool prependEnvironmentInQueueName = true) where TEvent : class {
        _queue.Enqueue(payload);
        return Task.CompletedTask;
    }
}
