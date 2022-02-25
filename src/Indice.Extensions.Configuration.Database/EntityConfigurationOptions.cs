using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// Configuration options for <see cref="EntityConfigurationProvider{T}"/>.
    /// </summary>
    public class EntityConfigurationOptions
    {
        /// <summary>
        /// The <see cref="TimeSpan"/> to wait in between each attempt at polling the database for changes. Default is null which indicates no reloading.
        /// </summary>
        public TimeSpan? ReloadOnInterval { get; set; }
        /// <summary>
        /// Represents a set of key/value application configuration properties.
        /// </summary>
        public IConfiguration Configuration { get; internal set; }
        /// <summary>
        /// Callback to configure the EF <see cref="DbContext"/>.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }

        internal EFConfigurationOptionsValidationResult Validate() {
            if (ReloadOnInterval.HasValue && ReloadOnInterval.Value <= TimeSpan.Zero) {
                return EFConfigurationOptionsValidationResult.Fail($"Property '{nameof(ReloadOnInterval)}' must have a positive value.");
            }
            return EFConfigurationOptionsValidationResult.Success();
        }
    }

    internal class EFConfigurationOptionsValidationResult
    {
        public bool Succedded { get; set; }
        public string Error { get; set; }

        public static EFConfigurationOptionsValidationResult Success() => new() { Succedded = true };

        public static EFConfigurationOptionsValidationResult Fail(string error) => new() { Error = error };
    }
}
