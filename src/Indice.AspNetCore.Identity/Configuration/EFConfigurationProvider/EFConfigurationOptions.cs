using System;
using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.EFCore
{
    /// <summary>
    /// Configuration options for <see cref="EFConfigurationProvider"/>.
    /// </summary>
    public class EFConfigurationOptions
    {
        /// <summary>
        /// The <see cref="TimeSpan"/> to wait in between each attempt at polling the database for changes. Default is null which indicates no reloading.
        /// </summary>
        public TimeSpan? ReloadInterval { get; set; }
        /// <summary>
        /// Configures the <see cref="DbContextOptions"/>.
        /// </summary>
        internal Action<DbContextOptionsBuilder> DbContextOptionsBuilder { get; set; }
    }
}
