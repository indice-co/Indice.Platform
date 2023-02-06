using Indice.Configuration;
using Indice.Features.Identity.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Indice.Features.Identity.Core.Data.Mappings
{
    /// <summary>Entity Framework mapping for type <see cref="ClaimType"/>.</summary>
    internal class ClaimTypeMap : IEntityTypeConfiguration<ClaimType>
    {
        /// <summary>Configure Entity Framework mapping for type <see cref="ClaimType"/>.</summary>
        /// <param name="builder">Provides a simple API for configuring an <see cref="EntityType"/>.</param>
        public void Configure(EntityTypeBuilder<ClaimType> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(ClaimType), "config");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure fields.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.S64).IsRequired();
            builder.Property(x => x.DisplayName).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.Description).HasMaxLength(TextSizePresets.L1024);
            builder.Property(x => x.Rule).HasMaxLength(TextSizePresets.M512);
            // Configure indexes.
            builder.HasIndex(x => x.Name);
        }
    }
}
