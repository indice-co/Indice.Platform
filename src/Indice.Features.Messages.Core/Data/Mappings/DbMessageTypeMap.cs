using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    /// <summary>
    /// Configuration for <see cref="DbMessageType"/> entity.
    /// </summary>
    public class DbMessageTypeMap : IEntityTypeConfiguration<DbMessageType>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbMessageTypeMap"/>.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbMessageTypeMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbMessageType> builder) {
            // Configure table name.
            builder.ToTable("MessageType", SchemaName);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M128).IsRequired();
            // Configure indexes.
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
