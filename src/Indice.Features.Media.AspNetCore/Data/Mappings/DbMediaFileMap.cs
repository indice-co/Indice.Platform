﻿using Indice.Features.Media.AspNetCore.Data.Models;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Indice.Configuration;
using Indice.Features.Media.AspNetCore.Models;

namespace Indice.Features.Media.AspNetCore.Data.Mappings;

/// <summary>Configuration for <see cref="DbMediaFile"/> entity.</summary>
public class DbMediaFileMap : IEntityTypeConfiguration<DbMediaFile>
{
    /// <summary>Creates a new instance of <see cref="DbMediaFileMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMediaFileMap(string schemaName) {
        _schemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }
    private string _schemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMediaFile> builder) {
        // Configure table name.
        builder.ToTable(nameof(MediaFile), _schemaName);
        // Configure key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.Name).IsUnique(false);
        builder.HasIndex(x => x.Path).IsUnique(true);
        // Configure Properties
        builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.Path).HasMaxLength(TextSizePresets.L1024).IsRequired();
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
