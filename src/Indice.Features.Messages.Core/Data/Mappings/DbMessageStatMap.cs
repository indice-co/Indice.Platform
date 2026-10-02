using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbMessageStat"/> entity.</summary>
public class DbMessageStatMap : IEntityTypeConfiguration<DbMessageStat>
{
    /// <summary>Creates a new instance of <see cref="DbMessageStatMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMessageStatMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMessageStat> builder) {
        builder.ToTable("MessageStat", SchemaName);
        // Configure primary key.
        builder.HasKey(x => new { x.Granularity, x.PeriodStart, x.Channel, x.CampaignTypeId });
        // Configure properties.
        builder.Property(x => x.CampaignTypeName).HasMaxLength(TextSizePresets.M128);
    }
}
