﻿using Indice.Configuration;
using Indice.Features.Cases.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Core.Data.Mappings;

internal class DbCaseTypeConfiguration : IEntityTypeConfiguration<DbCaseType>
{
    public void Configure(EntityTypeBuilder<DbCaseType> builder) {
        builder
            .ToTable("CaseType");
        builder
            .HasKey(p => p.Id);
        builder
            .HasIndex(p => p.Code);
        builder
            .Property(p => p.Code)
            .HasMaxLength(TextSizePresets.S64)
            .IsRequired();
        builder
            .Property(p => p.Title)
            .HasMaxLength(TextSizePresets.M128)
            .IsRequired();
        builder
            .Property(p => p.DataSchema)
            .HasJsonConversion()
            .IsRequired();
        builder
            .Property(p => p.Description)
            .HasMaxLength(TextSizePresets.M256)
            .IsRequired(false);
        builder
            .Property(p => p.Translations)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.Layout)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.LayoutTranslations)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.Tags)
            .HasMaxLength(TextSizePresets.M256)
            .IsRequired(false);
        builder
            .Property(p => p.Config)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.CanCreateRoles)
            .HasMaxLength(TextSizePresets.M256)
            .IsRequired(false);
        builder
            .Property(p => p.GridFilterConfig)
            .HasJsonConversion()
            .IsRequired(false);
        builder
            .Property(p => p.GridColumnConfig)
            .HasJsonConversion()
            .IsRequired(false);
    }
}