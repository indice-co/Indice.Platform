using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbMessageMap : IEntityTypeConfiguration<DbMessage>
    {
        public DbMessageMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbMessage> builder) {
            // Configure table name.
            builder.ToTable("Message", CampaignsApiOptions.DatabaseSchema);
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
