using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbMessage"/> entity.</summary>
public class DbMessageMap : IEntityTypeConfiguration<DbMessage>
{
    /// <summary>Creates a new instance of <see cref="DbMessageMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMessageMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMessage> builder) {
        // Configure table name.
        builder.ToTable("Message", SchemaName);
        // Configure primary keys.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Content).HasRequiredJsonConversion().IsRequired();
        builder.Property(x => x.RecipientId).IsRequired(false);
        // Configure indexes.
        var recipientIndex = builder.HasIndex(x => new { x.RecipientId, x.CampaignId }).IsUnique(false);
        SqlServerIndexBuilderExtensions.IncludeProperties(recipientIndex, x => new { x.IsDeleted, x.IsRead });
        NpgsqlIndexBuilderExtensions.IncludeProperties(recipientIndex, x => new { x.IsDeleted, x.IsRead });
    }
}
