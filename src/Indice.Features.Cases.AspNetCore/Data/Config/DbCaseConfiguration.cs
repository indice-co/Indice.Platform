using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbCaseConfiguration : IEntityTypeConfiguration<DbCase>
{
    public void Configure(EntityTypeBuilder<DbCase> builder) {
        builder
            .ToTable("Case");
        builder
            .HasKey(p => p.Id);
        builder
            .OwnsOneAudit(p => p.CreatedBy, required: true)
            .OwnsOneAudit(p => p.CompletedBy)
            .OwnsOneAudit(p => p.AssignedTo);
        builder
            .OwnsOne(
                p => p.Customer,
                actionBuilder => {
                    var prefix = nameof(DbCase.Customer);
                    actionBuilder
                        .Property(p => p.UserId)
                        .HasColumnName($"{prefix}{nameof(DbCase.Customer.UserId)}")
                        .HasMaxLength(TextSizePresets.S64);
                    actionBuilder
                        .Property(p => p.CustomerId)
                        .HasColumnName($"{prefix}{nameof(DbCase.Customer.CustomerId)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Property(p => p.FirstName)
                        .HasColumnName($"{prefix}{nameof(DbCase.Customer.FirstName)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Property(p => p.LastName)
                        .HasColumnName($"{prefix}{nameof(DbCase.Customer.LastName)}")
                        .HasMaxLength(TextSizePresets.M128);
                    actionBuilder
                        .Ignore(p => p.FullName);
                });
        builder
            .Property(p => p.GroupId)
            .HasMaxLength(TextSizePresets.M128);
        builder
            .HasMany(p => p.Checkpoints)
            .WithOne(p => p.Case)
            .HasForeignKey(p => p.CaseId);
        builder
            .HasOne(p => p.PublicCheckpoint)
            .WithMany()
            .HasForeignKey(p => p.PublicCheckpointId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .HasOne(p => p.Checkpoint)
            .WithMany()
            .HasForeignKey(p => p.CheckpointId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .HasOne(p => p.PublicData)
            .WithMany()
            .HasForeignKey(p => p.PublicDataId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
            .HasOne(p => p.Data)
            .WithMany()
            .HasForeignKey(p => p.DataId)
            .OnDelete(DeleteBehavior.NoAction);
        builder
           .Property(c => c.Metadata)
           .HasJsonConversion();
        builder
            .Property(c => c.Channel)
            .IsRequired()
            .HasMaxLength(TextSizePresets.M128);
        builder
            .HasMany(p => p.Versions)
            .WithOne(p => p.Case)
            .HasForeignKey(p => p.CaseId);
        builder
           .HasMany(p => p.StakeHolders)
           .WithOne(p => p.Case)
           .HasForeignKey(p => p.CaseId);
    }
}
