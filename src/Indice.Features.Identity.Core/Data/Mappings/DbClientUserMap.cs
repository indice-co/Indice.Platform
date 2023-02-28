using Indice.Features.Identity.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Indice.Features.Identity.Core.Data.Mappings;

/// <summary>Entity Framework mapping for type <see cref="DbClientUser"/>.</summary>
internal class DbClientUserMap : IEntityTypeConfiguration<DbClientUser>
{
    /// <summary>Configure Entity Framework mapping for type <see cref="DbClientUser"/>.</summary>
    /// <param name="builder">Provides a simple API for configuring an <see cref="EntityType"/>.</param>
    public void Configure(EntityTypeBuilder<DbClientUser> builder) {
        // Configure table name and schema.
        builder.ToTable(nameof(DbClientUser), "config");
        // Configure primary key.
        builder.HasKey(x => new { x.ClientId, x.UserId });
        // Configure relationships.
        builder.HasOne(x => x.Client).WithMany().HasForeignKey(x => x.ClientId);
    }
}
