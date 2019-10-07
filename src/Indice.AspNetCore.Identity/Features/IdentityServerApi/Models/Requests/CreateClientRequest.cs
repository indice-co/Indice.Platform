using System.Collections.Generic;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Models a client that will be created on the server.
    /// </summary>
    public class CreateClientRequest
    {
        /// <summary>
        /// Describes the type of the client.
        /// </summary>
        public ClientType ClientType { get; set; }
        /// <summary>
        /// The unique identifier for this application.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// Application name that will be seen on consent screens.
        /// </summary>
        public string ClientName { get; set; }
        /// <summary>
        /// Application URL that will be seen on consent screens.
        /// </summary>
        public string ClientUri { get; set; }
        /// <summary>
        /// Application logo that will be seen on consent screens.
        /// </summary>
        public string LogoUri { get; set; }
        /// <summary>
        /// Application description.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Specifies whether a consent screen is required.
        /// </summary>
        public bool RequireConsent { get; set; }
        /// <summary>
        /// The list of identity resources allowed by the client.
        /// </summary>
        public IEnumerable<string> IdentityResources { get; set; } = new List<string>();
        /// <summary>
        /// The list of API resources allowed by the client.
        /// </summary>
        public IEnumerable<string> ApiResources { get; set; } = new List<string>();
        /// <summary>
        /// Allowed URL to return after logging in.
        /// </summary>
        public string RedirectUri { get; set; }
        /// <summary>
        /// Allowed URL to return after logout.
        /// </summary>
        public string PostLogoutRedirectUri { get; set; }
        /// <summary>
        /// The client secrets.
        /// </summary>
        public List<ClientSecretRequest> Secrets { get; set; } = new List<ClientSecretRequest>();
    }

    /// <summary>
    /// Models an OAuth client type.
    /// </summary>
    public enum ClientType
    {
        /// <summary>
        /// Single page application supporting authorization code.
        /// </summary>
        SPA,
        /// <summary>
        /// Classic web application.
        /// </summary>
        WebApp,
        /// <summary>
        /// A desktop or mobile application running on a user's device.
        /// </summary>
        Native,
        /// <summary>
        /// A server to server application.
        /// </summary>
        Machine,
        /// <summary>
        /// IoT application or otherwise browserless or input constrained device.
        /// </summary>
        Device,
        /// <summary>
        /// Single page application supporting implicit flow.
        /// </summary>
        SPALegacy
    }
}
