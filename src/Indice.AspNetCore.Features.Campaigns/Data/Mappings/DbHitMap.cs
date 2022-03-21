using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbHitMap : IEntityTypeConfiguration<DbHit>
    {
        public DbHitMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

        public void Configure(EntityTypeBuilder<DbHit> builder) {
            // Configure table name.
            builder.ToTable("Hit", SchemaName);
            // Configure key.
            builder.HasKey(x => x.Id);
            // Configure indexes.
            builder.HasIndex(x => x.CampaignId);
            // Configure properties.
            builder.Property(x => x.CampaignId).IsRequired();
            builder.Property(x => x.TimeStamp).IsRequired();
        }
    }
}
