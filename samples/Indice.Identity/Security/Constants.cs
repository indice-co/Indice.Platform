namespace Indice.Identity.Security
{
    /// <summary>
    /// Constants that describe IdentityServer API scopes.
    /// </summary>
    public class IdentityServerApi
    {
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
