using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCheckpointConfiguration : IEntityTypeConfiguration<DbCheckpoint>
    {
        public void Configure(EntityTypeBuilder<DbCheckpoint> builder) {
            builder
                .ToTable("Checkpoint");
            builder
                .HasKey(p => p.Id);
            builder
                .OwnsOne(
                    p => p.CreatedBy,
                    actionBuilder => {
                        var prefix = nameof(DbCheckpoint.CreatedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCheckpoint.CreatedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCheckpoint.CreatedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCheckpoint.CreatedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCheckpoint.CreatedBy.When)}");
                    });
            builder
                .HasOne(p => p.CheckpointType)
                .WithMany()
                .HasForeignKey(p => p.CheckpointTypeId)
                .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
        }
    }
}