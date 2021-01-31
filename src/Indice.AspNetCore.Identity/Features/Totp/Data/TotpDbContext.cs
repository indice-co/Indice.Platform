using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// A <see cref="DbContext"/> used to maintain the generated OTP codes of a user.
    /// </summary>
    public class TotpDbContext : DbContext
    {
        /// <summary>
        /// Contains the relationship between a user a generated OTP code.
        /// </summary>
        public DbSet<UserTotp> UserTotp { get; set; }

        /// <summary>
        /// Register extended configuration methods when the database is being created.
        /// </summary>
        /// <param name="modelBuilder">Provides a simple API surface for configuring a <see cref="DbContext"/>.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UserTotpMap());
        }
    }
}
