using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Indice.AspNetCore.Identity.Features
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="ClaimType"/>.
    /// </summary>
    public class ClaimTypeMap : IEntityTypeConfiguration<ClaimType>
    {
        /// <summary>
        /// Configure Entity Framework mapping for type <see cref="ClaimType"/>.
        /// </summary>
        /// <param name="builder">Provides a simple API for configuring an <see cref="EntityType"/>.</param>
        public void Configure(EntityTypeBuilder<ClaimType> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(ClaimType), "auth");
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
