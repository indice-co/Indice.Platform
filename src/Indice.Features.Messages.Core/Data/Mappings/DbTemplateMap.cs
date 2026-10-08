using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings;

/// <summary>Configuration for <see cref="DbTemplate"/> entity.</summary>
public class DbTemplateMap : IEntityTypeConfiguration<DbTemplate>
{
    /// <summary>Creates a new instance of <see cref="DbTemplateMap"/>.</summary>
    /// <param name="schemaName">The schema name.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public DbTemplateMap(string schemaName) {
        SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
    }

    private string SchemaName { get; }

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<DbTemplate> builder) {
        // Configure table name.
        builder.ToTable("Template", SchemaName);
        // Configure primary key.
        builder.HasKey(x => x.Id);
        // Configure properties.
        builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(TextSizePresets.S64).IsRequired(false);
        builder.Property(x => x.IgnoreUserPreferences).IsRequired();
        builder.Property(x => x.Content).HasRequiredJsonConversion().IsRequired();
        builder.Property(x => x.Data).HasJsonConversion();
        builder.Property(x => x.Type).IsRequired().HasDefaultValue(TemplateType.Full);
        builder.Property(x => x.CreatedBy).HasMaxLength(TextSizePresets.M128).IsRequired();
        builder.Property(x => x.UpdatedBy).HasMaxLength(TextSizePresets.M128);
        // Configure indexes.
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.Alias).IsUnique();
        builder.HasOne(x => x.MessageType).WithMany().HasForeignKey(x => x.MessageTypeId);
    }
}
