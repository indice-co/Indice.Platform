using Indice.Configuration;
using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbDistributionList"/> entity.
    /// </summary>
    public class DbDistributionListMap : IEntityTypeConfiguration<DbDistributionList>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbDistributionListMap"/>.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbDistributionListMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbDistributionList> builder) {
            // Configure table name.
            builder.ToTable("DistributionList", SchemaName);
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M128).IsRequired();
            // Configure indexes.
            builder.HasIndex(x => x.Name);
        }
    }
}
