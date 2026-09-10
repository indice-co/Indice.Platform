using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Agents.Core.Data.Mappings;

/// <summary>EF Core configuration for <see cref="DbCheckpoint"/>.</summary>
public class DbCheckpointMap : IEntityTypeConfiguration<DbCheckpoint>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<DbCheckpoint> builder) {
        builder.ToTable("Checkpoint", "dex");
        builder.HasKey(x => new { x.SessionId, x.CheckpointId });
        builder.Property(x => x.SessionId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.CheckpointId).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.ParentCheckpointId).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.Payload).IsRequired();
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
    }
}
