using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbConversation"/>.</summary>
public class DbConversationMap : IEntityTypeConfiguration<DbConversation>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbConversation> builder) {
        builder.ToTable("Conversation", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.InputTokenCount).HasDefaultValue(0L);
        builder.Property(x => x.OutputTokenCount).HasDefaultValue(0L);
        builder.Property(x => x.MessageCount).HasDefaultValue(0);
        builder.HasIndex(x => new { x.UserId, x.LastActivityAt })
               .IsDescending(false, true);
        builder.HasMany(x => x.Messages)
               .WithOne()
               .HasForeignKey(m => m.ConversationId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
