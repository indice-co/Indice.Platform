using System.Diagnostics;
using System.Reflection;
using Indice.EntityFrameworkCore;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Data
{
    public class CasesDbContext : DbContext
    {
        public CasesDbContext(DbContextOptions<CasesDbContext> options) : base(options) {
            if (Debugger.IsAttached) {
                Database.EnsureCreated();
            }
        }

        public DbSet<DbCase> Cases => Set<DbCase>();
        public DbSet<DbQuery> Queries => Set<DbQuery>();
        public DbSet<DbAttachment> Attachments => Set<DbAttachment>();
        public DbSet<DbCaseType> CaseTypes => Set<DbCaseType>();
        public DbSet<DbCheckpoint> Checkpoints => Set<DbCheckpoint>();
        public DbSet<DbCheckpointType> CheckpointTypes => Set<DbCheckpointType>();
        public DbSet<DbComment> Comments => Set<DbComment>();
        public DbSet<DbRoleCaseType> RoleCaseTypes => Set<DbRoleCaseType>();
        public DbSet<DbCaseData> CaseData => Set<DbCaseData>();
        public DbSet<DbCaseTypeNotificationSubscription> CaseTypeNotificationSubscription => Set<DbCaseTypeNotificationSubscription>();
        public DbSet<DbCaseApproval> CaseApprovals => Set<DbCaseApproval>();
        public DbSet<DbCaseTypeCategory> CaseTypeCategories => Set<DbCaseTypeCategory>();

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyJsonFunctions();
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(CasesApiConstants.DatabaseSchema);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
