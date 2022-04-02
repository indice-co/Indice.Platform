using System.Diagnostics;
using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Indice.AspNetCore.Features.Campaigns.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Indice.AspNetCore.Features.Campaigns.Data
{
    /// <summary>
    /// The <see cref="DbContext"/> for Campaigns API feature.
    /// </summary>
    public class CampaignsDbContext : DbContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="CampaignsDbContext"/>.
        /// </summary>
        /// <param name="options">The options to be used by <see cref="CampaignsDbContext"/>.</param>
        public CampaignsDbContext(DbContextOptions<CampaignsDbContext> options) : base(options) {
            if (Debugger.IsAttached) {
                Database.EnsureCreated();
            }
        }

        /// <summary>
        /// Campaign attachments table.
        /// </summary>
        public DbSet<DbAttachment> Attachments { get; set; }
        /// <summary>
        /// Campaigns table.
        /// </summary>
        public DbSet<DbCampaign> Campaigns { get; set; }
        /// <summary>
        /// Message types table.
        /// </summary>
        public DbSet<DbMessageType> MessageTypes { get; set; }
        /// <summary>
        /// Inbox messages table.
        /// </summary>
        public DbSet<DbMessage> Messages { get; set; }
        /// <summary>
        /// Campaign hits table.
        /// </summary>
        public DbSet<DbHit> Hits { get; set; }
        /// <summary>
        /// Templates table.
        /// </summary>
        public DbSet<DbTemplate> Templates { get; set; }
        /// <summary>
        /// Distribution lists table.
        /// </summary>
        public DbSet<DbDistributionList> DistributionLists { get; set; }
        /// <summary>
        /// Contacts table
        /// </summary>
        public DbSet<DbContact> Contacts { get; set; }

        /// <inheritdoc />
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
