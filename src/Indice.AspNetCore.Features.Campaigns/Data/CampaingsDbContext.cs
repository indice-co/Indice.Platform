using Indice.AspNetCore.Features.Campaigns.Configuration;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    /// <summary>
    /// A <see cref="DbContext"/> that contains tables for campaign management system.
    /// </summary>
    internal class CampaingsDbContext : DbContext
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        public CampaingsDbContext(DbContextOptions<CampaingsDbContext> options) : base(options) {
#if DEBUG
            Database.EnsureCreated();
#endif
        }

        public DbSet<DbAttachment> Attachments { get; set; }
        public DbSet<DbCampaign> Campaigns { get; set; }
        public DbSet<DbCampaignType> CampaignTypes { get; set; }
        public DbSet<DbCampaignUser> CampaignUsers { get; set; }
        public DbSet<DbCampaignVisit> CampaignVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            var campaignsApiOptions = Database.GetService<IOptions<CampaignsApiOptions>>();
            builder.ApplyConfiguration(new DbAttachmentMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignTypeMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignUserMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignVisitMap(campaignsApiOptions));
        }
    }
}
