using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbMemberConfiguration : IEntityTypeConfiguration<DbMember>
{
    public void Configure(EntityTypeBuilder<DbMember> builder) {
        builder
            .ToTable("Member");
        builder
            .HasKey(p => p.Id);
        builder
            .Property(p => p.RoleName)
            .HasMaxLength(TextSizePresets.S64);
        builder
            .HasOne(p => p.CaseType)
            .WithMany()
            .HasForeignKey(p => p.CaseTypeId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
        builder
            .HasOne(p => p.CheckpointType)
            .WithMany()
            .HasForeignKey(p => p.CheckpointTypeId)
            .OnDelete(DeleteBehavior.NoAction);  // todo check if this leaves garbage behind!!!
    }
}