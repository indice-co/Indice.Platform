using Indice.Extensions.Configuration.Database.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Extensions.Configuration.Database.Data;

/// <summary>Database configuration for <see cref="DbAppSetting"/> entity.</summary>
public class AppSettingMap : IEntityTypeConfiguration<DbAppSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbAppSetting> builder) {
        // Configure table name and schema.
        builder.ToTable("AppSetting", "config");
        // Configure primary key.
        builder.HasKey(x => x.Key);
        // Configure fields.
        builder.Property(x => x.Key).HasMaxLength(512);
        builder.Property(x => x.Value).HasMaxLength(2048).IsRequired();
    }
}
