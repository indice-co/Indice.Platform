using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.Configuration;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    public class DbCampaignMap : IEntityTypeConfiguration<DbCampaign>
    {
        public void Configure(EntityTypeBuilder<DbCampaign> builder) {
            // Configure table name.
            builder.ToTable("Campaign", "dbo");
            // Configure primary key.
            builder.HasKey(x => x.Id);
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.Title).HasMaxLength(TextSizePresets.M128).IsRequired();
            builder.Property(x => x.Content).IsRequired();
            builder.Property(x => x.ActionText).HasMaxLength(TextSizePresets.M128);
            builder.Property(x => x.ActionUrl).HasMaxLength(TextSizePresets.L2048);
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.From).HasColumnName(nameof(Period.From));
            builder.OwnsOne(x => x.ActivePeriod).Property(x => x.To).HasColumnName(nameof(Period.To));
            builder.Property(x => x.IsGlobal).IsRequired();
            builder.Property(x => x.IsNotification).IsRequired();
            // Configure relationships.
            builder.HasOne(x => x.Attachment).WithMany().HasForeignKey(x => x.AttachmentId);
        }
    }
}