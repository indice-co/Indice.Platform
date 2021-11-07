using System;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignVisitMap : IEntityTypeConfiguration<DbCampaignVisit>
    {
        public DbCampaignVisitMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbCampaignVisit> builder) {
            // Configure table name.
            builder.ToTable("CampaignVisit", CampaignsApiOptions.DatabaseSchema);
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
