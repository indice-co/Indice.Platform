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
    public class IdentityDbContext : IdentityDbContext<User, IdentityRole>
    {
        /// <summary>
        /// Constructs the <see cref="DbContext"/> passing the options.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }
    }

    /// <summary>
    /// <see cref="DbContext"/> for the Identity Framework, supporting custom role type.
    /// </summary>
    /// <typeparam name="TUser">The type of the user to use.</typeparam>
    /// <typeparam name="TRole">The type of the role to use.</typeparam>
    public class IdentityDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole, string>
        where TUser : User 
        where TRole : IdentityRole
    {
        /// <summary>
        /// Constructs the <see cref="DbContext"/> passing the options.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }
        /// <summary>
        /// Stores all previous passwords of a user for future validation checks.
        /// </summary>
        public DbSet<UserPassword> UserPasswordHistory { get; set; }

        /// <summary>
        /// Configures schema needed for the Identity framework.
        /// </summary>
        /// <param name="builder">Class used to create and apply a set of data model conventions.</param>
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.ApplyConfiguration(new UserMap<TUser>());
            builder.Entity<TRole>().ToTable("Role", "auth");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaim", "auth");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaim", "auth");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRole", "auth");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserToken", "auth");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogin", "auth");
            builder.Entity<UserPassword>(innerBuilder => {
                innerBuilder.ToTable(nameof(UserPassword), "auth");
                innerBuilder.HasKey(x => x.Id);
                innerBuilder.HasOne<TUser>()
                            .WithMany()
                            .HasForeignKey(x => x.UserId)
                            .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
