using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbDistributionList"/> entity.</summary>
public class DbCommunicationPreferenceMessageTypeMap : IEntityTypeConfiguration<DbCommunicationPreferenceMessageType>
{
    /// <summary>Creates a new instance of <see cref="DbCommunicationPreferenceMessageType"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbCommunicationPreferenceMessageTypeMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbCommunicationPreferenceMessageType> builder) {
        // Configure table name.
        builder.ToTable("CommunicationPreferenceMessageType", SchemaName);
        // Configure composite primary key.
        builder.HasKey(x => new { x.DBContactPreferenceId, x.TypeId });
        // Configure properties.
        builder.Property(x => x.DBContactPreferenceId).IsRequired();
        builder.Property(x => x.TypeId).IsRequired();
        builder.Property(x => x.CommunicationPreferences).HasDefaultValue(ContactChannelKind.Any);
        // Configure relationships.
        builder.HasOne(x => x.CommunicationPreference).WithMany(x => x.MessageTypeCommunicationPreferences).HasForeignKey(x => x.DBContactPreferenceId);
        builder.HasOne(x => x.Type).WithMany(x => x.ContactPreferenceMessageTypes).HasForeignKey(x => x.TypeId);
    }
}
