using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.Database
{
    /// <summary>
    /// 
    /// </summary>
    public class AppSettingsDbContext : DbContext, IAppSettingsDbContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dbContextOptions"></param>
        public AppSettingsDbContext(DbContextOptions<AppSettingsDbContext> dbContextOptions) : base(dbContextOptions) { }

        /// <summary>
        /// 
        /// </summary>
        public DbSet<AppSetting> AppSettings { get; set; }

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new AppSettingMap());
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public interface IAppSettingsDbContext
    {
        /// <summary>
        /// 
        /// </summary>
        DbSet<AppSetting> AppSettings { get; set; }
    }
}
