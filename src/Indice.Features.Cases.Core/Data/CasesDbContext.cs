using System.Reflection;
using Indice.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Data;

/// <summary>Cases DbContext</summary>
public class CasesDbContext : DbContext
{
    /// <inheritdoc />
    public CasesDbContext(DbContextOptions<CasesDbContext> options) : base(options) {
        
    }

    /// <summary>Cases</summary>
    public DbSet<DbCase> Cases { get; set; } = null!;
    /// <summary>Saved queries</summary>
    public DbSet<DbQuery> Queries { get; set; } = null!;
    /// <summary>Attachments</summary>
    public DbSet<DbAttachment> Attachments { get; set; } = null!;
    /// <summary>Case types</summary>
    public DbSet<DbCaseType> CaseTypes { get; set; } = null!;
    /// <summary>Checkpoints</summary>
    public DbSet<DbCheckpoint> Checkpoints { get; set; } = null!;
    /// <summary>Checkpoint types</summary>
    public DbSet<DbCheckpointType> CheckpointTypes { get; set; } = null!;
    /// <summary>Comments</summary>
    public DbSet<DbComment> Comments { get; set; } = null!;
    /// <summary>CaseType members</summary>
    public DbSet<DbCaseAccessRule> CaseAccessRules { get; set; } = null!;
    /// <summary>Member Notifications Subscriptions</summary>
    /// <remarks>Per user subscriptions for case modification alerts via e-mail.</remarks>
    public DbSet<DbNotificationSubscription> NotificationSubscriptions { get; set; } = null!;
    /// <summary>Case data is the actual dynamic dataset that accompanies a <see cref="DbCase"/>. May have multiple versions for one case</summary>
    public DbSet<DbCaseData> CaseData { get; set; } = null!;
    /// <summary>Case approval. This is probably an approval log</summary>
    public DbSet<DbCaseApproval> CaseApprovals { get; set; } = null!;
    /// <summary>Case type category.</summary>
    public DbSet<DbCategory> Categories { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyJsonFunctions();
        modelBuilder.HasSequence<int>(CasesCoreConstants.ReferenceNumberSequence, CasesCoreConstants.DatabaseSchema);

        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(CasesCoreConstants.DatabaseSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    /// <summary>
    /// Gets the next value of the `ReferenceNumberSequence` sequence.
    /// </summary>
    public async Task<int> GetNextReferenceNumber() {
        var result = new SqlParameter("@result", System.Data.SqlDbType.Int) {
            Direction = System.Data.ParameterDirection.Output
        };
        await Database.ExecuteSqlRawAsync($"SET @result = NEXT VALUE FOR [{CasesCoreConstants.DatabaseSchema}].[{CasesCoreConstants.ReferenceNumberSequence}]", result);

        var value = (int)result.Value;
        return value;
    }
}
