using System;
using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbCampaignMap : IEntityTypeConfiguration<DbCampaign>
    {
        public DbCampaignMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbCampaign> builder) {
            // Configure table name.
            builder.ToTable("Campaign", CampaignsApiOptions.DatabaseSchema);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M128).IsRequired();
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.ActionText).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.ActionUrl).HasMaxLength(TextSizePresets.L2048);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.Published).IsRequired();
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.From).HasColumnName(nameof(Period.From));
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.To).HasColumnName(nameof(Period.To));
            builder.Property(x => x.IsGlobal).IsRequired();
            builder.Property(x => x.DataJson).HasColumnName(nameof(DbCampaign.Data));
            builder.Ignore(x => x.Data);
            // Configure relationships.
            builder.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId);
            builder.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.TypeId);
        }
    }
}
