using Indice.Features.Media.AspNetCore.Data.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Configuration;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;

/// <summary>Configuration for <see cref="DbFile"/> entity.</summary>
public class DbFileMap : IEntityTypeConfiguration<DbFile>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbFile> builder) {
        // Configure table name.
        builder.ToTable("File");
        // Configure key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.Name).IsUnique(false);
        // Configure Properties
        builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.FileExtension).HasMaxLength(TextSizePresets.S08).IsRequired();
        builder.Property(x => x.Data).HasColumnType("image");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
        builder.Ignore(x => x.Uri);
        // Configure relationships.
        builder.HasOne(x => x.Folder).WithMany(f => f.Files).HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.SetNull);
    }
}
