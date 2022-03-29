using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignUserMap : IEntityTypeConfiguration<DbCampaignUser>
    {
        public DbCampaignUserMap(string schemaName) {
            SchemaName = schemaName ?? throw new ArgumentNullException(nameof(schemaName));
        }

        public string SchemaName { get; }

        public void Configure(EntityTypeBuilder<DbCampaignUser> builder) {
            // Configure table name.
            builder.ToTable("CampaignUser", SchemaName);
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.CampaignId, x.UserCode });
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}
