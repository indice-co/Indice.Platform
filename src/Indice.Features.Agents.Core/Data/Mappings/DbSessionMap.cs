using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbSession"/>.</summary>
public class DbSessionMap : IEntityTypeConfiguration<DbSession>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbSession> builder) {
        builder.ToTable("Session", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.TotalPromptTokens).HasDefaultValue(0L);
        builder.Property(x => x.TotalCompletionTokens).HasDefaultValue(0L);
        builder.HasIndex(x => new { x.UserId, x.LastActivityAt })
               .IsDescending(false, true);
        builder.HasMany(x => x.Messages)
               .WithOne()
               .HasForeignKey(m => m.SessionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
