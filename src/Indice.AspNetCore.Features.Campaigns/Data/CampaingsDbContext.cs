using System.Diagnostics;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    internal class CampaignsDbContext : DbContext
    {
        public CampaignsDbContext(DbContextOptions<CampaignsDbContext> options) : base(options) {
            if (Debugger.IsAttached) {
                Database.EnsureCreated();
            }
        }

        public DbSet<DbAttachment> Attachments { get; set; }
        public DbSet<DbCampaign> Campaigns { get; set; }
        public DbSet<DbMessageType> MessageTypes { get; set; }
        public DbSet<DbMessage> Messages { get; set; }
        public DbSet<DbHit> Hits { get; set; }
        public DbSet<DbTemplate> Templates { get; set; }
        public DbSet<DbDistributionList> DistributionList { get; set; }
        public DbSet<DbContact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            var campaignsApiOptions = Database.GetService<IOptions<CampaignsApiOptions>>();
            builder.ApplyConfiguration(new DbAttachmentMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbHitMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbDistributionListMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbContactMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbMessageMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbMessageTypeMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbTemplateMap(campaignsApiOptions));
        }
    }
}
