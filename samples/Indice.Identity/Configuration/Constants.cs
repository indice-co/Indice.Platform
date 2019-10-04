namespace Indice.Identity.Configuration
{
    /// <summary>
    /// Preset sizes for database fields.
    /// </summary>
    public class TextSizePresets
    {
        /// <summary>
        /// Size 8.
        /// </summary>
        public const int S08 = 8;
        /// <summary>
        /// Size 16.
        /// </summary>
        public const int S16 = 16;
        /// <summary>
        /// Size 32.
        /// </summary>
        public const int S32 = 32;
        /// <summary>
        /// Size 64.
        /// </summary>
        public const int S64 = 64;
        /// <summary>
        /// Size 128.
        /// </summary>
        public const int M128 = 128;
        /// <summary>
        /// Size 256.
        /// </summary>
        public const int M256 = 256;
        /// <summary>
        /// Size 512.
        /// </summary>
        public const int M512 = 512;
        /// <summary>
        /// Size 1024.
        /// </summary>
        public const int L1024 = 1024;
        /// <summary>
        /// Size 2048.
        /// </summary>
        public const int L2048 = 2048;
        /// <summary>
        /// Size 4096.
        /// </summary>
        public const int L4096 = 4096;
    }

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
