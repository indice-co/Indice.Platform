using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Indice.AspNetCore.Authentication.GovGr
{
    /// <summary>
    /// Configuration options for GovGr OpenID Connect.
    /// </summary>
    public class GovGrOptions
    {
        /// <summary>
        /// The client id.
        /// </summary>
        public string ClientId { get; set; }
        /// <summary>
        /// The client secret.
        /// </summary>
        public string ClientSecret { get; set; }
        /// <summary>
        /// The request path within the application's base path where the user-agent will be returned. The middleware will process this request when it arrives.
        /// </summary>
        public PathString? CallbackPath { get; set; }
        /// <summary>
        /// Gets or sets the authentication scheme corresponding to the middleware responsible of persisting user's identity after a successful authentication. This value typically
        /// corresponds to a cookie middleware registered in the Startup class. When omitted, <see cref="AuthenticationOptions.DefaultSignInScheme"/> is used as a fallback value.
        /// </summary>
        public string SignInScheme { get; set; }
    }
}
