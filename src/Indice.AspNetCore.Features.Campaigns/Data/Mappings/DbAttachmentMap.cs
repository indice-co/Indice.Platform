using System;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class DbAttachmentMap : IEntityTypeConfiguration<DbAttachment>
    {
        public DbAttachmentMap(IOptions<CampaignsApiOptions> campaignsApiOptions) {
            CampaignsApiOptions = campaignsApiOptions?.Value ?? throw new ArgumentNullException(nameof(campaignsApiOptions));
        }

        public CampaignsApiOptions CampaignsApiOptions { get; }

        public void Configure(EntityTypeBuilder<DbAttachment> builder) {
            // Configure table name.
            builder.ToTable("Attachment", CampaignsApiOptions.DatabaseSchema);
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
            builder.Property(x => x.ContentType).HasMaxLength(TextSizePresets.M256).IsRequired();
            builder.Property(x => x.FileExtension).HasMaxLength(TextSizePresets.S08).IsRequired();
            builder.Property(x => x.Data).HasColumnType("image");
            builder.Ignore(x => x.Uri);
        }
    }
}
