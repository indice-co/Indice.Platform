using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Configuration;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;
/// <summary>Configuration for <see cref="DbMediaSetting"/> entity.</summary>
public class DbMediaSettingMap : IEntityTypeConfiguration<DbMediaSetting>
{
    /// <summary>Creates a new instance of <see cref="DbMediaSettingMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMediaSettingMap(string schemaName) {
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }
    private string _schemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMediaSetting> builder) {
        // Configure table name.
        builder.ToTable(nameof(MediaSetting), _schemaName);
        // Configure key.
        builder.HasKey(x => x.Key);
        // Configure Properties
        builder.Property(x => x.Key).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Value);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
    }
}
