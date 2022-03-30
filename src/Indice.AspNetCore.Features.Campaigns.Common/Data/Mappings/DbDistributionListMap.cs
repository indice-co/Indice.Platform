using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    public class DbDistributionListMap : IEntityTypeConfiguration<DbDistributionList>
    {
        public DbDistributionListMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

        public void Configure(EntityTypeBuilder<DbDistributionList> builder) {
            // Configure table name.
            builder.ToTable("DistributionList", SchemaName);
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M128).IsRequired();
            // Configure indexes.
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
