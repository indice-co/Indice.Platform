using System;
using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseApprovalConfiguration : IEntityTypeConfiguration<DbCaseApproval>
    {
        public void Configure(EntityTypeBuilder<DbCaseApproval> builder) {
            builder
                .ToTable("CaseApproval");
            builder
                .HasKey(p => p.Id);
            builder
                .Property(p => p.Action)
                .HasMaxLength(TextSizePresets.S64)
                .HasConversion(
                    valueToSave => valueToSave.ToString(),
                    valueRetrieved => (Approval)Enum.Parse(typeof(Approval), valueRetrieved))
                .IsRequired();
            builder
                .OwnsOne(
                    p => p.CreatedBy,
                    actionBuilder => {
                        var prefix = nameof(DbCaseApproval.CreatedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCaseApproval.CreatedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCaseApproval.CreatedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCaseApproval.CreatedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCaseApproval.CreatedBy.When)}");
                    });
            builder
                .Property(p => p.Reason)
                .HasMaxLength(TextSizePresets.M128);
        }
    }
}