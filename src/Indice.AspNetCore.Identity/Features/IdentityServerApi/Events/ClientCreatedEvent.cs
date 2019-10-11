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
        /// The instance of the client that was created.
        /// </summary>
        public ClientInfo Client { get; private set; }

        /// <summary>
        /// Creates a detailed string representation implementation of <see cref="ClientCreatedEvent"/>.
        /// </summary>
        public override string ToString() {
            var builder = new StringBuilder();
            builder.AppendLine("A client was created in IdentityServer API.")
                   .AppendLine($"Id: {Client.ClientId}, ")
                   .AppendLine($"Name: {Client.ClientName}, ")
                   .AppendLine($"Description: {Client.Description}, ")
                   .AppendLine($"Enabled: {Client.Enabled}, ")
                   .AppendLine($"AllowRememberConsent: {Client.AllowRememberConsent}, ")
                   .AppendLine($"ClientUri: {Client.ClientUri}, ")
                   .AppendLine($"LogoUri: {Client.LogoUri}, ")
                   .AppendLine($"RequireConsent: {Client.RequireConsent}");
            return builder.ToString();
        }
    }
}
