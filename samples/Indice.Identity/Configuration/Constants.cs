namespace Indice.Identity.Configuration
{
    /// <summary>
    /// Cache keys for storing items in the cache store.
    /// </summary>
    public class CacheKeys
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
