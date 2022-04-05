using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbTemplate"/> entity.
    /// </summary>
    public class DbTemplateMap : IEntityTypeConfiguration<DbTemplate>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbTemplateMap"/>.
        /// </summary>
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
            builder.Property(x => x.Content).HasJsonConversion();
            // Configure indexes.
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
