namespace Indice.AspNetCore.Features.Settings
{
    /// <summary>
    /// Constant values for Settings API.
    /// </summary>
    public class SettingsApi
    {
        /// <summary>
        /// Authentication scheme name used by Settings API.
        /// </summary>
        public const string AuthenticationScheme = "Bearer";
        /// <summary>
        /// Settings API scope.
        /// </summary>
        public const string Scope = "identity";
        /// <summary>
        /// Default database schema.
        /// </summary>
        public const string DatabaseSchema = "config";

        /// <summary>
        /// Settings API policies.
        /// </summary>
        public static class Policies
        {
            /// <summary>
            /// A user must be an <i>Admin</i>.
            /// </summary>
            public const string BeSettingsManager = nameof(BeSettingsManager);
        }
    }
}
