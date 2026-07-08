using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbDocumentBlob"/>.</summary>
public class DbDocumentBlobMap : IEntityTypeConfiguration<DbDocumentBlob>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbDocumentBlob> builder) {
        builder.ToTable("DocumentBlob", "dex");
        builder.HasKey(x => x.DocumentId);
        builder.Property(x => x.FileName).HasMaxLength(TextSizePresets.M512).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.HasOne(x => x.Document)
               .WithOne(x => x.Blob)
               .HasForeignKey<DbDocumentBlob>(x => x.DocumentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
