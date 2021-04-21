using Indice.AspNetCore.Identity.Api.Models;

namespace Indice.AspNetCore.Identity.Api.Events
{
    /// <summary>
    /// An event that is raised when a new client is created on IdentityServer.
    /// </summary>
    public class ClientCreatedEvent : IIdentityServerApiEvent
    {
        /// <summary>
        /// Creates a new instance of <see cref="ClientCreatedEvent"/>.
        /// </summary>
        /// <param name="client"></param>
        public ClientCreatedEvent(ClientInfo client) => Client = client;

        /// <summary>
        /// The instance of the client that was created.
        /// </summary>
        public ClientInfo Client { get; private set; }
    }
}
