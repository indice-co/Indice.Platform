using Indice.AspNetCore.Identity.Data.Mappings;
using Indice.AspNetCore.Identity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// <see cref="DbContext"/> for the Identity Framework.
    /// </summary>
    public class IdentityDbContext : IdentityDbContext<User>
    {
        /// <summary>
        /// Constructs the dbcontext passing the options.
        /// </summary>
        /// <param name="options"></param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }

        /// <summary>
        /// Stores all previous passwords of a user for future validation checks.
        /// </summary>
        public DbSet<UserPassword> UserPasswordHistory { get; set; }

        /// <summary>
        /// Configures schema needed for the Identity framework
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new UserMap());
            builder.Entity<IdentityRole>().ToTable("Role", "auth");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaim", "auth");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaim", "auth");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRole", "auth");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserToken", "auth");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogin", "auth");

            builder.Entity<UserPassword>(b => {
                b.ToTable(nameof(UserPassword), "auth");
                b.HasKey(x => x.Id);
                b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
