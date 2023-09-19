using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Features.Media.AspNetCore.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Configuration;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;
/// <summary>Configuration for <see cref="DbMediaSetting"/> entity.</summary>
public class DbMediaSettingMap : IEntityTypeConfiguration<DbMediaSetting>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMediaSetting> builder) {
        // Configure table name.
        builder.ToTable(nameof(MediaSetting));
        // Configure key.
        builder.HasKey(x => x.Key);
        // Configure Properties
        builder.Property(x => x.Key).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.Value);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
    }
}
