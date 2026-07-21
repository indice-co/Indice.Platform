using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbCitation"/>.</summary>
public class DbCitationMap : IEntityTypeConfiguration<DbCitation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbCitation> builder) {
        builder.ToTable("Citation", "dex");
        builder.HasKey(x => new { x.SessionMessageId, x.Number });
        
        builder.HasOne<DbSessionMessage>().WithMany(x => x.Citations)
            .HasForeignKey(x => x.SessionMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Chunk).WithMany()
            .HasForeignKey(x => x.ChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
