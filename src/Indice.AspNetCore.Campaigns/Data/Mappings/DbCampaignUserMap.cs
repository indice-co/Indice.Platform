using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    public class DbCampaignUserMap : IEntityTypeConfiguration<DbCampaignUser>
    {
        public void Configure(EntityTypeBuilder<DbCampaignUser> builder) {
            // Configure table name.
            builder.ToTable("CampaignUser", "dbo");
            // Configure primary keys.
            builder.HasKey(x => x.Id);
            builder.HasAlternateKey(x => new { x.CampaignId, x.UserCode });
            // Configure properties.
            builder.Property(x => x.Id).ValueGeneratedNever();
        }
    }
}