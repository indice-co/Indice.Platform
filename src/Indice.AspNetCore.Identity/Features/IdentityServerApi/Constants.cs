namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Constants that describe IdentityServer API scopes.
    /// </summary>
    public class IdentityServerApi
    {
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

    /// <summary>
    /// Cache keys for storing items in the cache store.
    /// </summary>
    internal class CacheKeys
    {
        /// <summary>
        /// News.
        /// </summary>
        public const string News = "news";
        /// <summary>
        /// Specified user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public static string User(string userId) => $"user_{userId.ToLower()}";
        /// <summary>
        /// Dashboard summary.
        /// </summary>
        public const string Summary = "summary";
    }
}
