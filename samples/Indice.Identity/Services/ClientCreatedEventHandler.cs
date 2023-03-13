using Indice.AspNetCore.Identity.Api.Events;
using Indice.Services;

namespace Indice.Identity.Services;

/// <summary>Handler for <see cref="ClientCreatedEvent"/> raised by IdentityServer API.</summary>
public class ClientCreatedEventHandler : IPlatformEventHandler<ClientCreatedEvent>
{
    private readonly ILogger<ClientCreatedEventHandler> _logger;

    /// <summary>Creates a new instance of <see cref="ClientCreatedEventHandler"/>.</summary>
    /// <param name="logger">Represents a type used to perform logging.</param>
    public ClientCreatedEventHandler(ILogger<ClientCreatedEventHandler> logger) {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Handle client created event.</summary>
    /// <param name="event">Client created event info.</param>
    public Task Handle(ClientCreatedEvent @event) {
        _logger.LogDebug($"Client created: {@event}");
        return Task.CompletedTask;
    }
}
