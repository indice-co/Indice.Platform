using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Indice.Features.Agents.Core.Models;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbMessage"/>.</summary>
public class DbMessageMap : IEntityTypeConfiguration<DbMessage>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbMessage> builder) {
        builder.ToTable("Message", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role)
               .HasConversion(
                    role => role.ToString().ToLowerInvariant(),
                    value => Enum.Parse<ChatMessageRole>(value, ignoreCase: true))
               .HasMaxLength(TextSizePresets.S32)
               .IsRequired();
        builder.ComplexProperty(b => b.Content, b => b.ToJson().IsRequired());
        builder.Property(x => x.ModelUsed).HasMaxLength(TextSizePresets.M128);
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
