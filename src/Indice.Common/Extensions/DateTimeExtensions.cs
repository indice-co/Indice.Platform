using System;

namespace Indice.Extensions
{
    /// <summary>
    /// Extensions on <see cref="DateTime"/> type.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Checks whether a time period has been exceeded when adding an amount of seconds.
        /// </summary>
        /// <param name="creationTime">The initial <see cref="DateTime"/>.</param>
        /// <param name="seconds">The amount of seconds to add.</param>
        /// <param name="now">The now.</param>
        public static bool HasExceeded(this DateTime creationTime, int seconds, DateTime now) => now > creationTime.AddSeconds(seconds);

        /// <summary>
        /// Gets the remaining lifetime in seconds, comparing two <see cref="DateTime"/> instances.
        /// </summary>
        /// <param name="creationTime">The initial <see cref="DateTime"/>.</param>
        /// <param name="now">The now.</param>
        public static int GetLifetimeInSeconds(this DateTime creationTime, DateTime now) => (int)(now - creationTime).TotalSeconds;

        /// <summary>
        /// Checks if a given <see cref="DateTime"/> has expired, compared to now.
        /// </summary>
        /// <param name="expirationTime">The initial <see cref="DateTime"/>.</param>
        /// <param name="now">The now.</param>
        public static bool HasExpired(this DateTime? expirationTime, DateTime now) {
            if (expirationTime.HasValue && expirationTime.Value.HasExpired(now)) {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if a given <see cref="DateTime"/> has expired, compared to now.
        /// </summary>
        /// <param name="expirationTime">The initial <see cref="DateTime"/>.</param>
        /// <param name="now">The now.</param>
        public static bool HasExpired(this DateTime expirationTime, DateTime now) {
            if (now > expirationTime) {
                return true;
            }
            return false;
        }
    }
}
