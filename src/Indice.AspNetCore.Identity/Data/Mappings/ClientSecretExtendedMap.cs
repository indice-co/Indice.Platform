using Indice.AspNetCore.Identity.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Identity.Data
{
    /// <summary>
    /// Entity Framework mapping for type <see cref="ClientSecretExtended"/>.
    /// </summary>
    internal class ClientSecretExtendedMap : IEntityTypeConfiguration<ClientSecretExtended>
    {
        /// <summary>
        /// Configure Entity Framework mapping for type <see cref="ClientSecretExtended"/>.
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(EntityTypeBuilder<ClientSecretExtended> builder) {
            // Configure table name and schema.
            builder.ToTable(nameof(ClientSecretExtended), "config");
            // Configure primary key.
            builder.HasKey(x => x.ClientSecretId);
            // Configure fields.
            builder.Property(x => x.CustomDataJson).HasColumnName(nameof(ClientSecretExtended.CustomData)).IsRequired();
            // Ignored properties.
            builder.Ignore(x => x.CustomData);
            // Configure relationships.
            builder.HasOne(x => x.ClientSecret).WithOne();
        }
    }
}
