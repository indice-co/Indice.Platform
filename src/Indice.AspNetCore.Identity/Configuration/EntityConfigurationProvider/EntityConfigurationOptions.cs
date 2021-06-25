using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Indice.Extensions.Configuration
{
    /// <summary>
    /// Configuration options for <see cref="EntityConfigurationProvider"/>.
    /// </summary>
    public class EntityConfigurationOptions
    {
        /// <summary>
        /// The <see cref="TimeSpan"/> to wait in between each attempt at polling the database for changes. Default is null which indicates no reloading.
        /// </summary>
        public TimeSpan? ReloadOnInterval { get; set; }
        /// <summary>
        /// Determines whether the application settings are reloaded when the corresponding database table changes. Default is true. This setting takes precedence over <see cref="ReloadOnInterval"/>.
        /// </summary>
        public bool ReloadOnDatabaseChange { get; set; } = true;
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
