using System.Diagnostics;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

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
        public DbSet<DbDistributionList> DistributionLists { get; set; }
        public DbSet<DbContact> Contacts { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            var schemaName = Database.GetService<DatabaseSchemaNameResolver>().GetSchemaName();
            builder.ApplyConfiguration(new DbAttachmentMap(schemaName));
            builder.ApplyConfiguration(new DbCampaignMap(schemaName));
            builder.ApplyConfiguration(new DbHitMap(schemaName));
            builder.ApplyConfiguration(new DbDistributionListMap(schemaName));
            builder.ApplyConfiguration(new DbContactMap(schemaName));
            builder.ApplyConfiguration(new DbMessageMap(schemaName));
            builder.ApplyConfiguration(new DbMessageTypeMap(schemaName));
            builder.ApplyConfiguration(new DbTemplateMap(schemaName));
        }
    }
}
