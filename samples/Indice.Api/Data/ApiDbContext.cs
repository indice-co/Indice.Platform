using Indice.Extensions.Configuration.Database;
using Indice.Extensions.Configuration.Database.Data;
using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Api.Data;

public class ApiDbContext : DbContext, IAppSettingsDbContext
{
    public ApiDbContext(DbContextOptions<ApiDbContext> dbContextOptions) : base(dbContextOptions) {
#if DEBUG
        Database.EnsureCreated();
#endif
    }

    public DbSet<AppSetting> AppSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new AppSettingMap());
    }
}
