using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// An extended <see cref="DbContext"/> for the Identity framework.
    /// </summary>
    public class ExtendedIdentityDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole>
        where TUser : User, new()
        where TRole : Role, new()
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedIdentityDbContext{TUser, TRole}"/>.
        /// </summary>
        /// <param name="dbContextOptions">The options to be used by a <see cref="DbContext"/>.</param>
        /// <param name="identityServerApiEndpointsOptions">Options for configuring the IdentityServer API feature.</param>
        public ExtendedIdentityDbContext(DbContextOptions<ExtendedIdentityDbContext<TUser, TRole>> dbContextOptions, IdentityServerApiEndpointsOptions identityServerApiEndpointsOptions) : base(dbContextOptions) {
#if DEBUG
            if (Database.EnsureCreated() && identityServerApiEndpointsOptions.UseInitialData) {
                this.Seed();
            }
#endif
        }

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
            modelBuilder.ApplyConfiguration(new ClaimTypeMap());
        }
    }
}
