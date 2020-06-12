using System.Security;

namespace Indice.Configuration
{
    /// <summary>
    /// Configuration used in <see cref="Rfc6238AuthenticationService"/> service.
    /// </summary>
    public class TotpOptions
    {
        /// <summary>
        /// The name is used to mark the section found inside a configuration file.
        /// </summary>
        public static readonly string Name = "Totp";
        /// <summary>
        /// Specifies the duration in seconds in which the one-time password is valid. Default is 1 minute. For security reasons this value cannot exceed 6 minutes.
        /// </summary>
        public int? TokenDuration { get; set; }
        /// <summary>
        /// An interval which will be used to calculate the value of the validity window.
        /// </summary>
        public double? Timestep => TokenDuration.HasValue ? (TokenDuration.Value / 2.0) : default(double?);
    }
}
