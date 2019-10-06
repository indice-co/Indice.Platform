using Indice.AspNetCore.Identity.Data;
using Indice.AspNetCore.Identity.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// An extended <see cref="DbContext"/> for the Identity framework.
    /// </summary>
    public sealed class ExtendedIdentityDbContext : IdentityDbContext<User, Role>
    {
        /// <summary>
        /// Creates a new instance of <see cref="ExtendedIdentityDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public ExtendedIdentityDbContext(DbContextOptions<ExtendedIdentityDbContext> options) : base(options) {
#if DEBUG
            if (Database.EnsureCreated()) {
                //this.Seed();
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
