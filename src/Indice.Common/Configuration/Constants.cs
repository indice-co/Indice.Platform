namespace Indice.Configuration
{
    /// <summary>
    /// Commonly used sizes for database fields.
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
    /// Contains names for custom headers used.
    /// </summary>
    public class CustomHeaderNames 
    {
        /// <summary>
        /// Header name for requests that need to contain an antiforgery token.
        /// </summary>
        public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    }
}
