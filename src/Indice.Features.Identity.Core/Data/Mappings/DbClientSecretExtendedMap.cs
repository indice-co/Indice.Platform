using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="DbClientSecretExtended"/>.</summary>
internal class DbClientSecretExtendedMap : IEntityTypeConfiguration<DbClientSecretExtended>
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbClientSecretExtended"/>.</summary>
    /// <param name="builder"></param>
    public void Configure(EntityTypeBuilder<DbClientSecretExtended> builder) {
        // Configure table name and schema.
        builder.ToTable(nameof(DbClientSecretExtended), "config");
        // Configure primary key.
        builder.HasKey(x => x.ClientSecretId);
        // Configure fields.
        builder.Property(x => x.CustomDataJson).HasColumnName(nameof(DbClientSecretExtended.CustomData)).IsRequired();
        // Ignored properties.
        builder.Ignore(x => x.CustomData);
        // Configure relationships.
        builder.HasOne(x => x.ClientSecret).WithOne();
    }
}
