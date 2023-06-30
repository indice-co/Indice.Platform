using System.Diagnostics;
using Indice.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Data;

/// <summary>A <see cref="DbContext"/> for persisting events and it's related data.</summary>
/// <typeparam name="TRiskEvent">The type of risk event.</typeparam>
public class RiskDbContext<TRiskEvent> : DbContext where TRiskEvent : DbRiskEvent
{
    /// <summary>Creates a new instance of <see cref="RiskDbContext{TTransaction}"/> class.</summary>
    /// <param name="dbContextOptions"></param>
    public RiskDbContext(DbContextOptions<RiskDbContext<TRiskEvent>> dbContextOptions) : base(dbContextOptions) {
        if (Debugger.IsAttached) {
            Database.EnsureCreated();
        }
    }

    /// <summary>Risk events table.</summary>
    public DbSet<TRiskEvent> RiskEvents => Set<TRiskEvent>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        // Risk event configuration.
        modelBuilder.Entity<TRiskEvent>().ToTable("RiskEvent");
        modelBuilder.Entity<TRiskEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<TRiskEvent>().HasIndex(x => x.CreatedAt);
        modelBuilder.Entity<TRiskEvent>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<TRiskEvent>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<TRiskEvent>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<TRiskEvent>().Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
        modelBuilder.Entity<TRiskEvent>().Property(x => x.SubjectId).HasMaxLength(TextSizePresets.M256).IsRequired();
    }
}
