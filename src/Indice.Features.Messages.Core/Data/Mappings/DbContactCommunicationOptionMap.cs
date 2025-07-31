using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbDistributionList"/> entity.</summary>
public class DbContactCommunicationOptionMap : IEntityTypeConfiguration<DbContactCommunicationOption>
{
    /// <summary>Creates a new instance of <see cref="DbContactCommunicationOption"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbContactCommunicationOptionMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbContactCommunicationOption> builder) {
        // Configure table name.
        builder.ToTable("ContactCommunicationOption", SchemaName);
        // Configure composite primary key.
        builder.HasKey(x => new { x.CommunicationPreferenceId, x.MessageTypeId });
        // Configure properties.
        builder.Property(x => x.CommunicationPreferenceId).IsRequired();
        builder.Property(x => x.MessageTypeId).IsRequired();
        builder.Property(x => x.CommunicationPreferences).HasDefaultValue(ContactChannelKind.Any);
        // Configure relationships.
        builder.HasOne(x => x.ContactPreference).WithMany(x => x.CommunicationOptions).HasForeignKey(x => x.CommunicationPreferenceId);
        builder.HasOne(x => x.MessageType).WithMany().HasForeignKey(x => x.MessageTypeId);
    }
}
