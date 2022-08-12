using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseDataConfiguration : IEntityTypeConfiguration<DbCaseData>
    {
        public void Configure(EntityTypeBuilder<DbCaseData> builder) {
            builder
                .ToTable("CaseData");
            builder
                .HasKey(p => p.Id);
            builder
                .OwnsOne(
                    p => p.CreatedBy,
                    actionBuilder => {
                        var prefix = nameof(DbCaseData.CreatedBy);
                        actionBuilder
                            .Property(p => p.Id)
                            .HasColumnName($"{prefix}{nameof(DbCaseData.CreatedBy.Id)}")
                            .HasMaxLength(TextSizePresets.S64);
                        actionBuilder
                            .Property(p => p.Email)
                            .HasColumnName($"{prefix}{nameof(DbCaseData.CreatedBy.Email)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.Name)
                            .HasColumnName($"{prefix}{nameof(DbCaseData.CreatedBy.Name)}")
                            .HasMaxLength(TextSizePresets.M128);
                        actionBuilder
                            .Property(p => p.When)
                            .HasColumnName($"{prefix}{nameof(DbCaseData.CreatedBy.When)}");
                    });
        }
    }
}