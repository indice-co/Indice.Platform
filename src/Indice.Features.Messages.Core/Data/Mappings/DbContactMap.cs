using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbContact"/> entity.</summary>
public class DbContactMap : IEntityTypeConfiguration<DbContact>
{
    /// <summary>Creates a new instance of <see cref="DbContactMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbContactMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbContact> builder) {
        builder.ToTable("Contact", SchemaName);
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Salutation).HasMaxLength(TextSizePresets.S32);
        builder.Property(x => x.FirstName).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.LastName).HasMaxLength(TextSizePresets.M128);
        builder.Property(x => x.FullName).HasMaxLength(TextSizePresets.M256);
        builder.Property(x => x.PhoneNumber).HasMaxLength(TextSizePresets.S64);
        builder.Property(x => x.Email).HasMaxLength(TextSizePresets.S64);
        builder.Property(x => x.RecipientId).HasMaxLength(TextSizePresets.S64);
        // Configure indexes.
        builder.HasIndex(x => x.RecipientId).IsUnique(true);
    }
}
