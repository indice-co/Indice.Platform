using System;
using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Extensions.Configuration.Database.Data;

/// <summary>
/// Default implementation of the Database AppSettings store.
/// </summary>
/// <param name="dbContextOptions"></param>
internal class DefaultAppSettingsDbContext(DbContextOptions<DefaultAppSettingsDbContext> dbContextOptions) 
    : DbContext(dbContextOptions), IAppSettingsDbContext
{
    /// <inheritdoc/>
    public DbSet<DbAppSetting> AppSettings { get; set; }

    /// <summary>Configures schema needed for the Identity framework.</summary>
    /// <param name="builder">Class used to create and apply a set of data model conventions.</param>
    protected override void OnModelCreating(ModelBuilder builder) {
        base.OnModelCreating(builder);
        builder.ApplyConfiguration(new AppSettingMap());
    }
}
