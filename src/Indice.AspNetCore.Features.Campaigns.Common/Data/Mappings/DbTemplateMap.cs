using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    public class DbTemplateMap : IEntityTypeConfiguration<DbTemplate>
    {
        public DbTemplateMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

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
