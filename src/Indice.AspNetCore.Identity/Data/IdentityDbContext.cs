using Indice.AspNetCore.Identity.Data.Models;
using Indice.Extensions.Configuration.Database;
using Indice.Extensions.Configuration.Database.Data;
using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// <see cref="DbContext"/> for the Identity Framework.
    /// </summary>
    public class IdentityDbContext : IdentityDbContext<User, Role>
    {
        /// <summary>
        /// Constructs the <see cref="IdentityDbContext"/> passing the options.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="IdentityDbContext"/>.</param>
        public IdentityDbContext(DbContextOptions options) : base(options) { }
    }

    /// <summary>
    /// <see cref="DbContext"/> for the Identity Framework, supporting custom role type.
    /// </summary>
    /// <typeparam name="TUser">The type of the user to use.</typeparam>
    /// <typeparam name="TRole">The type of the role to use.</typeparam>
    public class IdentityDbContext<TUser, TRole> : IdentityDbContext<TUser, TRole, string>, IAppSettingsDbContext
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
        /// Stores user devices in database.
        /// </summary>
        public DbSet<UserDevice> UserDevices { get; set; }
        /// <summary>
        /// Application settings stored in the database.
        /// </summary>
        public DbSet<AppSetting> AppSettings { get; set; }

        /// <summary>
        /// Configures schema needed for the Identity framework.
        /// </summary>
        /// <param name="builder">Class used to create and apply a set of data model conventions.</param>
        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            builder.Entity<TRole>().ToTable("Role", "auth");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaim", "auth");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaim", "auth");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRole", "auth");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserToken", "auth");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogin", "auth");
            builder.ApplyConfiguration(new UserMap<TUser>());
            builder.ApplyConfiguration(new UserPasswordMap<TUser>());
            builder.ApplyConfiguration(new UserDeviceMap<TUser>());
            builder.ApplyConfiguration(new AppSettingMap());
        }
    }
}
