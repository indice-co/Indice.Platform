using System;
using System.Collections.Generic;
using System.Text;
using Indice.Configuration;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Cases.Data.Config
{
    internal class DbCaseTypeCategoryConfiguration : IEntityTypeConfiguration<DbCaseTypeCategory>
    {
        public void Configure(EntityTypeBuilder<DbCaseTypeCategory> builder) {
            builder
                .ToTable("CaseTypeCategory");
            builder
                .HasKey(p => p.Id);
            builder
                .Property(p => p.Name)
                .HasMaxLength(TextSizePresets.M128);
            builder
                .Property(p => p.Description)
                .HasMaxLength(TextSizePresets.M512);
            builder
                .Property(p => p.Order);
        }
    }
}
