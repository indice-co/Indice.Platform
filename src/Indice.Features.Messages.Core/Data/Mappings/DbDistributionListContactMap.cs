using Indice.Features.Messages.Core.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.Features.Messages.Core.Data.Mappings
{
    /// <summary>Configuration for <see cref="DbDistributionListContact"/> entity.</summary>
    public class DbDistributionListContactMap : IEntityTypeConfiguration<DbDistributionListContact>
    {
        /// <summary>Creates a new instance of <see cref="DbDistributionListContactMap"/>.</summary>
        /// <param name="schemaName">The schema name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public DbDistributionListContactMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        private string SchemaName { get; }

        /// <inheritdoc />
        public void Configure(EntityTypeBuilder<DbDistributionListContact> builder) {
            // Configure table name.
            builder.ToTable("DistributionListContact", SchemaName);
            // Configure primary key.
            builder.HasKey(x => new { x.ContactId, x.DistributionListId });
            // Configure relationships.
            builder.HasOne(x => x.DistributionList).WithMany(x => x.ContactDistributionLists).HasForeignKey(x => x.DistributionListId);
            builder.HasOne(x => x.Contact).WithMany(x => x.DistributionListContacts).HasForeignKey(x => x.ContactId);
        }
    }
}
