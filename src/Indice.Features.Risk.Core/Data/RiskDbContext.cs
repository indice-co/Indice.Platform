#nullable disable
using Indice.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Data;

/// <summary>A <see cref="DbContext"/> for persisting transactions and it's related data.</summary>
/// <typeparam name="TTransaction">The type of transaction.</typeparam>
public class RiskDbContext<TTransaction> : DbContext where TTransaction : TransactionBase
{
    /// <summary></summary>
    public DbSet<TTransaction> Transactions { get; set; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<TTransaction>().ToTable("Transaction");
        modelBuilder.Entity<TTransaction>().HasKey(x => x.Id);
        modelBuilder.Entity<TTransaction>().HasIndex(x => x.SubjectId);
        modelBuilder.Entity<TTransaction>().Property(x => x.Amount).HasColumnType("money");
        modelBuilder.Entity<TTransaction>().Property(x => x.IpAddress).HasMaxLength(TextSizePresets.M128);
    }
}
#nullable enable
