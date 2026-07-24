using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbBlob"/>.</summary>
public class DbBlobMap : IEntityTypeConfiguration<DbBlob>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbBlob> builder) {
        builder.ToTable("Blob", "dex");
        builder.HasKey(x => x.DocumentId);
        builder.Property(x => x.FileName).HasMaxLength(TextSizePresets.M512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.ETag).HasMaxLength(TextSizePresets.S64).IsFixedLength().IsRequired();
        builder.Property(x => x.LastModified).IsConcurrencyToken().IsRequired();
        builder.HasOne(x => x.Document)
               .WithOne(x => x.Blob)
               .HasForeignKey<DbBlob>(x => x.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
