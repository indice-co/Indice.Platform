using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbHit"/> entity.</summary>
public class DbHitMap : IEntityTypeConfiguration<DbHit>
{
    /// <summary>Creates a new instance of <see cref="DbHitMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbHitMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbHit> builder) {
        // Configure table name.
        builder.ToTable("Hit", SchemaName);
        // Configure key.
        builder.HasKey(x => x.Id);
        // Configure indexes.
        builder.HasIndex(x => x.CampaignId);
        // Configure properties.
        builder.Property(x => x.CampaignId).IsRequired();
        builder.Property(x => x.TimeStamp).IsRequired();
    }
}
