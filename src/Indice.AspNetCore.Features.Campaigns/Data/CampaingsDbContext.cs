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

        public DbSet<DbCampaignAttachment> Attachments { get; set; }
        public DbSet<DbCampaign> Campaigns { get; set; }
        public DbSet<DbNotificationType> NotificationTypes { get; set; }
        public DbSet<DbNotification> Notifications { get; set; }
        public DbSet<DbCampaignVisit> CampaignVisits { get; set; }
        public DbSet<DbTemplate> Templates { get; set; }
        public DbSet<DbDistributionList> DistributionList { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            var campaignsApiOptions = Database.GetService<IOptions<CampaignsApiOptions>>();
            builder.ApplyConfiguration(new DbCampaignAttachmentMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbCampaignVisitMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbDistributionListMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbContactMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbNotificationMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbNotificationTypeMap(campaignsApiOptions));
            builder.ApplyConfiguration(new DbTemplateMap(campaignsApiOptions));
        }
    }
}
