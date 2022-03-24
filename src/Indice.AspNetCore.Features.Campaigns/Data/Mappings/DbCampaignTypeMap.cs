using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignTypeMap : IEntityTypeConfiguration<DbCampaignType>
    {
        public DbCampaignTypeMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

        public void Configure(EntityTypeBuilder<DbCampaignType> builder) {
            // Configure table name.
            builder.ToTable("CampaignType", SchemaName);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure indexes.
            builder.HasIndex(x => x.Name);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.L1024).IsRequired();
        }
    }
}
