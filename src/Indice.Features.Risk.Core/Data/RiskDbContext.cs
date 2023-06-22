using System.Diagnostics;
using Indice.Configuration;
using Indice.Features.Risk.Core.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Data;

/// <summary>A <see cref="DbContext"/> for persisting transactions and it's related data.</summary>
/// <typeparam name="TTransaction">The type of transaction.</typeparam>
public class RiskDbContext<TTransaction> : DbContext where TTransaction : Transaction
{
    /// <summary>Creates a new instance of <see cref="RiskDbContext{TTransaction}"/> class.</summary>
    /// <param name="dbContextOptions"></param>
    public RiskDbContext(DbContextOptions<RiskDbContext<TTransaction>> dbContextOptions) : base(dbContextOptions) {
        if (Debugger.IsAttached) {
            Database.EnsureCreated();
        }
    }

    /// <summary>Transactions table.</summary>
    public DbSet<TTransaction> Transactions => Set<TTransaction>();
    /// <summary>Transaction events table.</summary>
    public DbSet<TransactionEvent> TransactionEvents { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        // Transaction configuration.
        modelBuilder.Entity<TTransaction>().ToTable(nameof(Transaction));
        modelBuilder.Entity<TTransaction>().HasKey(x => x.Id);
        modelBuilder.Entity<TTransaction>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<TTransaction>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<TTransaction>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
        modelBuilder.Entity<TTransaction>().HasMany(x => x.Events).WithOne(x => (TTransaction)x.Transaction).HasForeignKey(x => x.TransactionId);
        // TransactionEvent configuration.
        modelBuilder.Entity<TransactionEvent>().ToTable(nameof(TransactionEvent));
        modelBuilder.Entity<TransactionEvent>().HasKey(x => x.Id);
        modelBuilder.Entity<TransactionEvent>().Property(x => x.Name).HasMaxLength(TextSizePresets.M256).IsRequired();
    }
}
