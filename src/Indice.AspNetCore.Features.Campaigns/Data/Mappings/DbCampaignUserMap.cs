using System;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignUserMap : IEntityTypeConfiguration<DbCampaignUser>
    {
        public DbCampaignUserMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbCampaignUser> builder) {
            // Configure table name.
            builder.ToTable("CampaignUser", CampaignsApiOptions.DatabaseSchema);
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.CampaignId, x.UserCode });
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}
