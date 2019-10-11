using System.Text;

namespace Indice.AspNetCore.Identity.Features
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
        /// The instance of client that was created.
        /// </summary>
        public ClientInfo Client { get; private set; }

        /// <summary>
        /// Creates a string implementation of <see cref="ClientCreatedEvent"/>.
        /// </summary>
        public override string ToString() {
            var builder = new StringBuilder();
            builder.Append("Client | ");
            builder.Append($"Id: {Client.ClientId}, ");
            builder.Append($"Name: {Client.ClientName}, ");
            builder.Append($"Description: {Client.Description}, ");
            builder.Append($"Enabled: {Client.Enabled}, ");
            builder.Append($"AllowRememberConsent: {Client.AllowRememberConsent}, ");
            builder.Append($"ClientUri: {Client.ClientUri}, ");
            builder.Append($"LogoUri: {Client.LogoUri}, ");
            builder.Append($"RequireConsent: {Client.RequireConsent}");
            return builder.ToString();
        }
    }
}
