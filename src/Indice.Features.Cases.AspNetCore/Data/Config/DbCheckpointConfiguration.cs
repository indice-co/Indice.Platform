using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbCheckpointConfiguration : IEntityTypeConfiguration<DbCheckpoint>
{
    public void Configure(EntityTypeBuilder<DbCheckpoint> builder) {
        builder
            .ToTable("Checkpoint");
        builder
            .HasKey(p => p.Id);
        builder
            .OwnsOneAudit(p => p.CreatedBy, required: true);
        builder
            .HasOne(p => p.CheckpointType)
            .WithMany()
            .HasForeignKey(p => p.CheckpointTypeId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
    }
}