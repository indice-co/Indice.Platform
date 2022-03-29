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

        public DbSet<DbCampaignAttachment> Attachments { get; set; }
        public DbSet<DbCampaign> Campaigns { get; set; }
        public DbSet<DbCampaignType> CampaignTypes { get; set; }
        public DbSet<DbCampaignUser> CampaignUsers { get; set; }
        public DbSet<DbCampaignVisit> CampaignVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) {
            var schemaNameResolver = Database.GetService<DatabaseSchemaNameResolver>();
            var schemaName = schemaNameResolver.GetSchemaName();
            builder.ApplyConfiguration(new DbCampaignAttachmentMap(schemaName));
            builder.ApplyConfiguration(new DbCampaignMap(schemaName));
            builder.ApplyConfiguration(new DbCampaignTypeMap(schemaName));
            builder.ApplyConfiguration(new DbCampaignUserMap(schemaName));
            builder.ApplyConfiguration(new DbCampaignVisitMap(schemaName));
        }
    }
}
