using Indice.AspNetCore.Features.Campaigns.Data.Models;
using Microsoft.EntityFrameworkCore;

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

        public DbSet<DbCampaign> Campaigns { get; set; }
        public DbSet<DbCampaignAttachment> CampaignAttachments { get; set; }
        public DbSet<DbCampaignUser> CampaignUsers { get; set; }
        public DbSet<DbCampaignVisit> CampaignVisits { get; set; }

        protected override void OnModelCreating(ModelBuilder builder) => builder.ApplyConfigurationsFromAssembly(typeof(DbCampaign).Assembly);
    }
}