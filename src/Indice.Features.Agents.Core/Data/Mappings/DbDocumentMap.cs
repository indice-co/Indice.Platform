using Indice.Configuration;
using Indice.Features.Agents.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbDocument"/>.</summary>
public class DbDocumentMap : IEntityTypeConfiguration<DbDocument>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbDocument> builder) {
        builder.ToTable("Document", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M512).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(TextSizePresets.L1024).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Language).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.ContentHash).HasMaxLength(TextSizePresets.S64).IsFixedLength().IsRequired();
        builder.Property(x => x.EmbeddingModel).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.Status).HasDefaultValue(DocumentStatus.Pending);
        builder.Property(x => x.ChunkCount).HasDefaultValue(0);
        builder.Property(x => x.IsPrivate).HasDefaultValue(false);
        builder.HasIndex(x => x.Source).IsUnique();
        builder.HasMany(x => x.Chunks)
               .WithOne()
               .HasForeignKey(c => c.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
