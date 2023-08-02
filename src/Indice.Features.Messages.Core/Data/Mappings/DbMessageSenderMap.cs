using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbMessageType"/> entity.</summary>
public class DbMessageSenderMap : IEntityTypeConfiguration<DbMessageSender>
{
    /// <summary>Creates a new instance of <see cref="DbMessageSenderMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbMessageSenderMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbMessageSender> builder) {
        // Configure table name.
        builder.ToTable("MessageSender", SchemaName);
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Sender).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired(); 
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
        // Configure indexes.
        builder.HasIndex(x => x.Sender);
    }
}
