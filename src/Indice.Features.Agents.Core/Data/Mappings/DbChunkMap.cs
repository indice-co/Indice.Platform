using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbChunk"/>.</summary>
public class DbChunkMap : IEntityTypeConfiguration<DbChunk>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbChunk> builder) {
        builder.ToTable("Chunk", "dex");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex }).IsUnique();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M512);
        builder.Property(x => x.Source).HasMaxLength(TextSizePresets.L1024);
        builder.Property(x => x.ContentHash).HasMaxLength(TextSizePresets.S64).IsFixedLength().IsRequired();
        builder.Property(x => x.Category).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Language).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.HeadingPath).HasMaxLength(TextSizePresets.L1024);
        builder.Property(x => x.EmbeddingModel).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.Embedding)
               .HasColumnType($"vector({AgentsOptions.EmbeddingDimensionsDefault})")
               .IsRequired();

        builder.HasOne(x => x.Document)
               .WithMany()
               .HasForeignKey(c => c.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
