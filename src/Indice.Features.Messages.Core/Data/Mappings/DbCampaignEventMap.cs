using System.Threading.Channels;
using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbCampaignEvent"/> entity.</summary>
public class DbCampaignEventMap : IEntityTypeConfiguration<DbCampaignEvent>
{
    /// <summary>Creates a new instance of <see cref="DbCampaignEventMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbCampaignEventMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbCampaignEvent> builder) {
        builder.ToTable("CampaignEvent", SchemaName);
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Type).HasMaxLength(TextSizePresets.S64);
        builder.Property(x => x.Channel).HasMaxLength(TextSizePresets.S64);
    }
}