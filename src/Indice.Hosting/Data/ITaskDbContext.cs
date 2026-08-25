using Indice.Hosting.Data.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Indice.Hosting.Data;

/// <summary>
/// <summary>A <see cref="DbContext"/> for hosting multiple <see cref="IMessageQueue{T}"/>.</summary>
/// Implement it on your own <see cref="DbContext"/> and register it through <c>UseStoreRelational&lt;TContext&gt;()</c>.
/// </summary>
/// <remarks>
/// <code>
/// public class BankingDbContext : DbContext, ITaskDbContext
/// {
///     public DbSet&lt;Order&gt; Orders { get; set; }
///
///     protected override void OnModelCreating(ModelBuilder builder) {
///         base.OnModelCreating(builder);
///         builder.ApplyWorkerStoreConfiguration(Database.ProviderName);
///     }
/// }
/// </code>
/// The <c>ApplyWorkerStoreConfiguration</c> call is required.
/// </remarks>
public interface ITaskDbContext
{
    /// <summary>Queue messages.</summary>
    DbSet<DbQMessage> Queue => Set<DbQMessage>();
    /// <summary>Tasks.</summary>
    DbSet<DbScheduledTask> Tasks => Set<DbScheduledTask>();
    /// <summary>Locks.</summary>
    DbSet<DbLock> Locks => Set<DbLock>();
    /// <inheritdoc cref="DbContext.Database"/>
    DatabaseFacade Database { get; }
    /// <inheritdoc cref="DbContext.Add{TEntity}"/>
    EntityEntry<TEntity> Add<TEntity>(TEntity entity) where TEntity : class;
    /// <inheritdoc cref="DbContext.Set{TEntity}()"/>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    /// <inheritdoc cref="DbContext.AddRange(object[])"/>
    void AddRange(IEnumerable<object> entities);
    /// <inheritdoc cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
