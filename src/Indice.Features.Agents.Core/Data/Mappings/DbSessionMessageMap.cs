using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbSessionMessage"/>.</summary>
public class DbSessionMessageMap : IEntityTypeConfiguration<DbSessionMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbSessionMessage> builder) {
        builder.ToTable("SessionMessage", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role)
               .HasConversion(
                    role => role.ToString().ToLowerInvariant(),
                    value => Enum.Parse<ChatMessageRole>(value, ignoreCase: true))
               .HasMaxLength(TextSizePresets.S32)
               .IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ModelUsed).HasMaxLength(TextSizePresets.M128);
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
