using System.Security.AccessControl;
using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCaseAccessRuleConfiguration : IEntityTypeConfiguration<DbCaseAccessRule>
{
    public void Configure(EntityTypeBuilder<DbCaseAccessRule> builder) {
        builder
            .ToTable("AccessRule");
        builder
            .HasKey(p => p.Id);

        builder
             .Property(p => p.MemberRole)
             .HasColumnName("M_Role")
             .HasMaxLength(TextSizePresets.S64);
        builder
            .Property(p => p.MemberGroupId)
            .HasColumnName("M_GroupId")
            .HasMaxLength(TextSizePresets.S64);
        builder
            .Property(p => p.MemberUserId)
            .HasColumnName("M_UserId")
            .HasMaxLength(TextSizePresets.S64);

        builder
            .Property(p => p.RuleCaseId)
            .HasColumnName("R_CaseId");
        builder
            .Property(p => p.RuleCaseTypeId)
            .HasColumnName("R_CaseTypeId");
        builder
            .Property(p => p.RuleCheckpointTypeId)
            .HasColumnName("R_CheckpointTypeId");

        builder
            .HasOne(p => p.CaseType)
            .WithMany()
            .HasForeignKey(p => p.RuleCaseTypeId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
        builder
            .HasOne(p => p.CheckpointType)
            .WithMany()
            .HasForeignKey(p => p.RuleCheckpointTypeId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
        builder
            .HasOne(p => p.Case)
            .WithMany()
            .HasForeignKey(p => p.RuleCaseId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
    }
}