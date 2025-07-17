using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbDistributionList"/> entity.</summary>
public class DbCommunicationPreferenceMap : IEntityTypeConfiguration<DbRecipientPreference>
{
    /// <summary>Creates a new instance of <see cref="DbRecipientPreference"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbCommunicationPreferenceMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbRecipientPreference> builder) {
        // Configure table name.
        builder.ToTable("CommunicationPreference", SchemaName);
        // Configure primary keys.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Id).IsRequired();
        builder.Property(x => x.RecipientId).HasMaxLength(TextSizePresets.S64).IsRequired();
        builder.Property(x => x.Locale).HasMaxLength(TextSizePresets.S16);
        builder.Property(x => x.ConsentCommercial);
        builder.Property(x => x.ConsentCommercialDate);
        // Configure indexes.
        builder.HasIndex(u => u.RecipientId).IsUnique();
    }
}