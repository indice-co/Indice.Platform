using IdentityServer4.Models;
using Indice.Services;

namespace Indice.Features.Identity.Core.Events;

/// <summary>An event that is raised when a new client is created on IdentityServer.</summary>
public class ClientCreatedEvent : IPlatformEvent
{
    /// <summary>Creates a new instance of <see cref="ClientCreatedEvent"/>.</summary>
    /// <param name="client">The client entity.</param>
    public ClientCreatedEvent(Client client) => Client = client;

    /// <summary>The client entity.</summary>
    public Client Client { get; }
}
