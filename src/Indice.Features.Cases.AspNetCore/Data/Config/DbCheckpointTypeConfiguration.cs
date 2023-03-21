using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config;

internal class DbCheckpointTypeConfiguration : IEntityTypeConfiguration<DbCheckpointType>
{
    public void Configure(EntityTypeBuilder<DbCheckpointType> builder) {
        builder
            .ToTable("CheckpointType");
        builder
            .HasKey(p => p.Id);
        builder
            .HasIndex(p => new { p.CaseTypeId, p.Code });
        builder
            .Property(p => p.Code)
            .HasMaxLength(TextSizePresets.M256)
            .IsRequired();
        builder
            .Property(p => p.Description)
            .HasMaxLength(TextSizePresets.M512);
    }
}