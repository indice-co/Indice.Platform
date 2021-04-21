using System.Reflection;

namespace Indice.AspNetCore.Identity.Api.Security
{
    /// <summary>
    /// Constants for IdentityServer API feature.
    /// </summary>
    public static class IdentityServerApi
    {
        /// <summary>
        /// The assembly name.
        /// </summary>
        public static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        /// <summary>
        /// Authentication scheme name used by IdentityServer local API.
        /// </summary>
        public const string AuthenticationScheme = "IdentityServerApiAccessToken";
        /// <summary>
        /// Identity API scope.
        /// </summary>
        public const string Scope = "identity";
        /// <summary>
        /// Admin of Identity system.
        /// </summary>
        public const string Admin = "identity-admin";

        /// <summary>
        /// Identity API sub-scopes.
        /// </summary>
        public static class SubScopes
        {
            /// <summary>
            /// A scope that allows managing clients on IdentityServer.
            /// </summary>
            public const string Clients = "identity:clients";
            /// <summary>
            /// A scope that allows managing users on IdentityServer.
            /// </summary>
            public const string Users = "identity:users";
        }
    }
}
