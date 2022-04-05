using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbMessage"/> entity.
    /// </summary>
    public class DbMessageMap : IEntityTypeConfiguration<DbMessage>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbMessageMap"/>.
        /// </summary>
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
            builder.HasAlternateKey(x => new { x.CampaignId, x.RecipientId });
            // Configure properties.
            builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M256);
            builder.Property(x => x.Body);
            // Configure indexes.
            builder.HasIndex(x => x.RecipientId);
        }
    }
}
