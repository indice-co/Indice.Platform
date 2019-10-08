using System;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Options for configuring IdentityDbContext.
    /// </summary>
    public class IdentityDbContextOptions
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
