using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Options;
using Indice.Features.Identity.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Indice.Features.Identity.Core.Data
{
    /// <summary>
    /// Extended DbContext for the IdentityServer configuration data.
    /// </summary>
    public class ExtendedConfigurationDbContext : ConfigurationDbContext<ExtendedConfigurationDbContext>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedConfigurationDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        /// <param name="storeOptions">Options for configuring the <see cref="ExtendedConfigurationDbContext"/>.</param>
        /// <param name="webHostEnvironment">Provides information about the web hosting environment an application is running in.</param>
        /// <param name="apiOptions">Options for configuring the IdentityServer API feature.</param>
        public ExtendedConfigurationDbContext(
            DbContextOptions<ExtendedConfigurationDbContext> options,
            ConfigurationStoreOptions storeOptions,
            IWebHostEnvironment webHostEnvironment
        ) : base(options, storeOptions) {
            if (webHostEnvironment.IsDevelopment() && Database.EnsureCreated()) {
                this.SeedInitialClaimTypes();
                // TODO: This should not be driven by the API enpoint options.
                //if (apiOptions.CustomClaims.Any()) {
                //    this.SeedCustomClaimTypes(apiOptions.CustomClaims);
                //}
            }
        }

        /// <summary>
        /// A table that contains the association between a client and a user.
        /// </summary>
        public DbSet<ClientUser> ClientUsers { get; set; }
        /// <summary>
        /// A table that contains custom data for a client secret.
        /// </summary>
        public DbSet<ClientSecretExtended> ClientSecretExtras { get; set; }
        /// <summary>
        /// A table that contains all the available claim types of the application.
        /// </summary>
        public DbSet<ClaimType> ClaimTypes { get; set; }

        /// <summary>
        /// Register extended configuration methods when the database is being created.
        /// </summary>
        /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new ClientUserMap());
            modelBuilder.ApplyConfiguration(new ClientSecretExtendedMap());
            modelBuilder.ApplyConfiguration(new ClaimTypeMap());
        }
    }
}
