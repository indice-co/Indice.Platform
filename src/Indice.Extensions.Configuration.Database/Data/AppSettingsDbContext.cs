using System.Diagnostics;
using Indice.Extensions.Configuration.Database.Data;
using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.Database
{
    internal sealed class AppSettingsDbContext : DbContext, IAppSettingsDbContext
    {
        public AppSettingsDbContext(DbContextOptions<AppSettingsDbContext> dbContextOptions) : base(dbContextOptions) {
            if (Debugger.IsAttached) {
                Database.EnsureCreated();
            }
        }

        public DbSet<AppSetting> AppSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfiguration(new AppSettingMap());
        }
    }

    /// <summary>
    /// Models the required interface needed to be inherited by a <see cref="DbContext"/> when an applications wants to use the database configuration feature.
    /// </summary>
    public interface IAppSettingsDbContext
    {
        /// <summary>
        /// The settings table.
        /// </summary>
        DbSet<AppSetting> AppSettings { get; set; }
    }
}
