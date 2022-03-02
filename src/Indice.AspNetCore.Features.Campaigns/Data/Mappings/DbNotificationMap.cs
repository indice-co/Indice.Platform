using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbNotificationMap : IEntityTypeConfiguration<DbNotification>
    {
        public DbNotificationMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbNotification> builder) {
            // Configure table name.
            builder.ToTable("Notification", CampaignsApiOptions.DatabaseSchema);
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.CampaignId, x.UserCode });
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}
