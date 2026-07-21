using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbProfile"/>.</summary>
public class DbProfileMap : IEntityTypeConfiguration<DbProfile>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbProfile> builder) {
        builder.ToTable("Profile", "dex");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.Email).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.Locale).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.PreferredLanguage).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.ResponseStyle).HasMaxLength(TextSizePresets.S32);
        builder.HasIndex(x => x.SubjectId).IsUnique();
    }
}
