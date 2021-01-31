using System;
using System.Security;
using Indice.AspNetCore.Identity.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Configuration
{
    /// <summary>
    /// Configuration used in <see cref="Rfc6238AuthenticationService"/> service.
    /// </summary>
    public class TotpOptions
    {
        internal IServiceCollection Services;
        /// <summary>
        /// The default code duration;
        /// </summary>
        public const int DefaultCodeDuration = 1;
        /// <summary>
        /// The default code length;
        /// </summary>
        public const int DefaultCodeLength = 6;
        /// <summary>
        /// The name is used to mark the section found inside a configuration file.
        /// </summary>
        public static readonly string Name = "Totp";
        /// <summary>
        /// Specifies the duration in seconds in which the one-time password is valid. Default is 1 minute. For security reasons this value cannot exceed 6 minutes.
        /// </summary>
        public int CodeDuration { get; set; } = DefaultCodeDuration;
        /// <summary>
        /// An interval which will be used to calculate the value of the validity window.
        /// </summary>
        public double Timestep => CodeDuration / 2.0;
        /// <summary>
        /// Indicates the length of the OTP code. Defaults to 6.
        /// </summary>
        public int CodeLength { get; set; } = DefaultCodeLength;
        /// <summary>
        /// Enables saving OTPs in the database (used mainly for development purposes). Defaults to false.
        /// </summary>
        public bool EnablePersistedCodes { get; set; } = false;
    }

    /// <summary>
    /// Options for configuring <see cref="TotpDbContext"/>.
    /// </summary>
    public class TotpDbContextOptions
    {
        /// <summary>
        /// Callback to configure the EF DbContext.
        /// </summary>
        public Action<DbContextOptionsBuilder> ConfigureDbContext { get; set; }
        /// <summary>
        /// Callback in DI to resolve the EF DbContextOptions. If set, ConfigureDbContext will not be used.
        /// </summary>
        public Action<IServiceProvider, DbContextOptionsBuilder> ResolveDbContextOptions { get; set; }
    }
}
