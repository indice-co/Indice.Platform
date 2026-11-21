using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbFile"/>.</summary>
public class DbFileMap : IEntityTypeConfiguration<DbFile>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbFile> builder) {
        builder.ToTable("File", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.FileName).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(TextSizePresets.S64).IsFixedLength().IsRequired();
        builder.Property(x => x.Data).IsRequired();
        builder.HasIndex(x => new { x.ConversationId, x.CreatedAt });
        builder.HasOne<DbConversation>()
               .WithMany()
               .HasForeignKey(x => x.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
