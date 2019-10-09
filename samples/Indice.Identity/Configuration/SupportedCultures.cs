using System.Collections.Generic;
using System.Globalization;

namespace Indice.Identity.Configuration
{
    /// <summary>
    /// Describes the cultures supported by the application.
    /// </summary>
    public class SupportedCultures
    {
        private static readonly IEnumerable<CultureInfo> _cultures = new List<CultureInfo> {
            new CultureInfo("en"),
            new CultureInfo("en-US"),
            new CultureInfo("en-GB")
        };

        /// <summary>
        /// The application's default culture.
        /// </summary>
        public const string Default = "en";

        /// <summary>
        /// Gets all cultures supported by the application.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<CultureInfo> Get() => _cultures;
    }
}
