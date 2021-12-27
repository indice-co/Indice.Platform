using System;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignTypeMap : IEntityTypeConfiguration<DbCampaignType>
    {
        public DbCampaignTypeMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbCampaignType> builder) {
            // Configure table name.
            builder.ToTable("CampaignType", CampaignsApiOptions.DatabaseSchema);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure indexes.
            builder.HasIndex(x => x.Name);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.L1024).IsRequired();
        }
    }
}
