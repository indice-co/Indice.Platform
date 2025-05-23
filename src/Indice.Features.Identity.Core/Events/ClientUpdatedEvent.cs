using Indice.Events;
using Indice.Features.Identity.Core.Events.Models;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new client is updated on IdentityServer.</summary>
/// <remarks>Creates a new instance of <see cref="ClientUpdatedEvent"/>.</remarks>
/// <param name="client">The client context.</param>
public class ClientUpdatedEvent(ClientEventContext client) : IPlatformEvent
{
    /// <summary>The client entity.</summary>
    public ClientEventContext Client { get; } = client;
}
