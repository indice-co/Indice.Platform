using System.Diagnostics;
using System.Reflection;
using Indice.EntityFrameworkCore;
using Indice.Features.Cases.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Data
{
    /// <summary>
    /// Cases DbContext
    /// </summary>
    public class CasesDbContext : DbContext
    {
        /// <inheritdoc />
        public CasesDbContext(DbContextOptions<CasesDbContext> options) : base(options) {
            if (Debugger.IsAttached) {
                Database.EnsureCreated();
            }
        }

        /// <summary>Cases</summary>
        public DbSet<DbCase> Cases => Set<DbCase>();
        /// <summary>Saved queries</summary>
        public DbSet<DbQuery> Queries => Set<DbQuery>();
        /// <summary>Attachments</summary>
        public DbSet<DbAttachment> Attachments => Set<DbAttachment>();
        /// <summary>Case types</summary>
        public DbSet<DbCaseType> CaseTypes => Set<DbCaseType>();
        /// <summary>Checkpoints</summary>
        public DbSet<DbCheckpoint> Checkpoints => Set<DbCheckpoint>();
        /// <summary>Checkpoint types</summary>
        public DbSet<DbCheckpointType> CheckpointTypes => Set<DbCheckpointType>();
        /// <summary>Comments</summary>
        public DbSet<DbComment> Comments => Set<DbComment>();
        /// <summary>Role case types? This is not very obvious name</summary>
        public DbSet<DbRoleCaseType> RoleCaseTypes => Set<DbRoleCaseType>();
        /// <summary>Case data is the actual dynamic dataset that accompanies a <see cref="DbCase"/>. May have multiple versions for one case</summary>
        public DbSet<DbCaseData> CaseData => Set<DbCaseData>();
        /// <summary>CaseTypeNotificationSubscription! Beats me</summary>
        public DbSet<DbCaseTypeNotificationSubscription> CaseTypeNotificationSubscription => Set<DbCaseTypeNotificationSubscription>();
        /// <summary>Case approval. This is probably an approval log</summary>
        public DbSet<DbCaseApproval> CaseApprovals => Set<DbCaseApproval>();
        /// <summary>Case type category.</summary>
        public DbSet<DbCaseTypeCategory> CaseTypeCategories => Set<DbCaseTypeCategory>();

        /// <inheritdoc />
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyJsonFunctions();
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema(CasesApiConstants.DatabaseSchema);
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
