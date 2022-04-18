using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>
    /// Configuration for <see cref="DbContactDistributionList"/> entity.
    /// </summary>
    public class DbContactDistributionListMap : IEntityTypeConfiguration<DbContactDistributionList>
    {
        /// <summary>
        /// Creates a new instance of <see cref="DbContactDistributionListMap"/>.
        /// </summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbContactDistributionListMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbContactDistributionList> builder) {
            // Configure table name.
            builder.ToTable("ContactDistributionList", SchemaName);
            // Configure primary key.
            builder.HasKey(x => new { x.ContactId, x.DistributionListId });
            // Configure relationships.
            builder.HasOne(x => x.DistributionList).WithMany(x => x.ContactDistributionLists).HasForeignKey(x => x.DistributionListId);
            builder.HasOne(x => x.Contact).WithMany(x => x.ContactDistributionLists).HasForeignKey(x => x.ContactId);
        }
    }
}
