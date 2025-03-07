using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCategoryConfiguration : IEntityTypeConfiguration<DbCategory>
{
    public void Configure(EntityTypeBuilder<DbCategory> builder) {
        builder
            .ToTable("Category");
        builder
            .HasKey(p => p.Id);
        builder
            .Property(p => p.Name)
            .HasMaxLength(TextSizePresets.M128);
        builder
            .Property(p => p.Description)
            .HasMaxLength(TextSizePresets.M512);
        builder
            .Property(p => p.Translations)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.Order);
    }
}
