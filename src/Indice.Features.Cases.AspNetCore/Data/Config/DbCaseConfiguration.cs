using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseConfiguration : IEntityTypeConfiguration<DbCase>
    {
        public void Configure(EntityTypeBuilder<DbCase> builder) {
            builder
                .ToTable("Case");
            builder
                .HasKey(p => p.Id);
            builder
                .OwnsOne(
                    p => p.CreatedBy,
                    actionBuilder => {
                        var prefix = nameof(DbCase.CreatedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCase.CreatedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCase.CreatedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCase.CreatedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCase.CreatedBy.When)}");
                    })
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
                .OwnsOne(
                    p => p.CompletedBy,
                    actionBuilder => {
                        var prefix = nameof(DbCase.CompletedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCase.CompletedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCase.CompletedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCase.CompletedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCase.CompletedBy.When)}");
                    }
                );
            builder
                .OwnsOne(
                    p => p.AssignedTo,
                    actionBuilder => {
                        var prefix = nameof(DbCase.AssignedTo);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCase.AssignedTo.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCase.AssignedTo.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCase.AssignedTo.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCase.AssignedTo.When)}");
                    }
                );
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
               .Property(c => c.Metadata)
               .HasJsonConversion();
            builder
                .Property(c => c.Channel)
                .IsRequired()
                .HasMaxLength(TextSizePresets.M128);
        }
    }
}
