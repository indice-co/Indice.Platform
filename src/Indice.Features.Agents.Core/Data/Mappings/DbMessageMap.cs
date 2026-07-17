using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.AI;

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
                    role => role.ToString(),
                    value => new ChatRole(value))
               .HasMaxLength(TextSizePresets.S32)
               .IsRequired();

        //TODO: check whats going on here with this version. builder.ComplexProperty(b => b.Content, b => b.ToJson().IsRequired());
        //builder.ComplexProperty(b => b.Contents, b => b.ToJson().IsRequired());
        builder.Property(x => x.Contents).HasRequiredJsonConversion();

        builder.Property(x => x.ModelUsed).HasMaxLength(TextSizePresets.M128);
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
