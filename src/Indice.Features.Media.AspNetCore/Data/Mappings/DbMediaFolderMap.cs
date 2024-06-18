using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Media.AspNetCore.Data.Models;
using Indice.Configuration;
using Indice.Features.Media.AspNetCore.Models;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;

/// <summary>Configuration for <see cref="DbMediaFolder"/> entity.</summary>
public class DbMediaFolderMap : IEntityTypeConfiguration<DbMediaFolder>
{
    /// <summary>Creates a new instance of <see cref="DbMediaFolderMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMediaFolderMap(string schemaName) {
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }
    private string _schemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMediaFolder> builder) {
        // Configure table name.
        builder.ToTable(nameof(MediaFolder), _schemaName);
        // Configure key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.Name).IsUnique(false);
        builder.HasIndex(x => x.Path).IsUnique(true);
        // Configure Properties
        builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Path).HasMaxLength(TextSizePresets.L1024).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
        // Configure relationships.
        builder.HasOne(x => x.Parent).WithMany(f => f.SubFolders).HasForeignKey(x => x.ParentId);
    }
}
