using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Configuration;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;

/// <summary>Configuration for <see cref="DbFolder"/> entity.</summary>
public class DbFolderMap : IEntityTypeConfiguration<DbFolder>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbFolder> builder) {
        // Configure table name.
        builder.ToTable("Folder");
        // Configure key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.Name).IsUnique(false);
        // Configure Properties
        builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Description).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
        // Configure relationships.
        builder.HasOne(x => x.Parent).WithMany(f => f.SubFolders).HasForeignKey(x => x.ParentId);
    }
}
