using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbMessageMap : IEntityTypeConfiguration<DbMessage>
    {
        public DbMessageMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

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
