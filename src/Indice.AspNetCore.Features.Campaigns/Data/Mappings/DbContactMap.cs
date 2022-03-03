using System;
using System.Collections.Generic;
using System.Text;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{

    internal class DbContactMap : IEntityTypeConfiguration<DbContact>
    {
        public DbContactMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbContact> builder) {
            builder.ToTable("Contact", CampaignsApiOptions.DatabaseSchema);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Salutation).HasMaxLength(TextSizePresets.S32);
            builder.Property(x => x.FirstName).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.LastName).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.FullName).HasMaxLength(TextSizePresets.M256);
            builder.Property(x => x.PhoneNumber).HasMaxLength(TextSizePresets.S64);
            builder.Property(x => x.Email).HasMaxLength(TextSizePresets.S64);
            builder.Property(x => x.DeviceId).HasMaxLength(TextSizePresets.M128);

            builder.Property(x => x.RecipientId).HasMaxLength(TextSizePresets.S64);

            builder.HasIndex(x => x.RecipientId);

            builder.HasOne<DbDistributionList>()
                   .WithMany(x => x.Contacts)
                   .HasForeignKey(x => x.DistributionListId);
        }
    }
}
