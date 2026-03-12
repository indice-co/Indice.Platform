using Indice.Features.ActivityLogs.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Options;
using Polly;

namespace Indice.Features.ActivityLogs.EntityFrameworkCore;

/// <summary><see cref="DbContext"/> for the Entity Framework Core that stores all user activity log data.</summary>
internal class ActivityLogDbContext : DbContext
{
    /// <summary>Constructs the <see cref="ActivityLogDbContext"/> passing the configured options.</summary>
    /// <param name="options">The options to be used by a <see cref="ActivityLogDbContext"/>.</param>
    public ActivityLogDbContext(DbContextOptions<ActivityLogDbContext> options) : base(options) {
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <summary>Stores all sign log entries.</summary>
    public DbSet<DbActivityLogEntry> ActivityLogs { get; set; }

    /// <summary>Configures schema needed for the Entity Framework Core.</summary>
    /// <param name="modelBuilder">Class used to create and apply a set of data model conventions.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        var schemaName = Database.GetService<IOptions<ActivityLogOptions>>().Value.DatabaseSchema;
        modelBuilder.ApplyConfiguration(new DbActivityLogEntryMap(schemaName));
    }
}
