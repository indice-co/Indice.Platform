using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// 
    /// </summary>
    public class ExtendedConfigurationDbContext : ConfigurationDbContext<ExtendedConfigurationDbContext>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedConfigurationDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        /// <param name="storeOptions">Options for configuring the <see cref="ExtendedConfigurationDbContext"/>.</param>
        public ExtendedConfigurationDbContext(DbContextOptions<ExtendedConfigurationDbContext> options, ConfigurationStoreOptions storeOptions) : base(options, storeOptions) {
#if DEBUG
            Database.EnsureCreated();
#endif
        }

        /// <summary>
        /// A table that contains the association between a client and a user.
        /// </summary>
        public DbSet<ClientUser> ClientUsers { get; set; }

        /// <summary>
        /// Register extended configuration methods when the database is being created.
        /// </summary>
        /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ClientUserMap());
        }
    }
}
