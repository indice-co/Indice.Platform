using Indice.Hosting.Models;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

/// <summary>OutboxDbContextExtensions</summary>
public static class OutboxDbContextExtensions
{
    /// <summary><inheritdoc cref="SaveAndEnqueueAsync{T}(DbContext, IMessageQueue{T}, IEnumerable{T}, DateTime?, CancellationToken)"/></summary>
    public static Task<int> SaveAndEnqueueAsync<T>(this DbContext callerContext, IMessageQueue<T> queue, T item, DateTime? enqueueAt = null, CancellationToken cancellationToken = default) where T : class 
        => callerContext.SaveAndEnqueueAsync(queue, [item], enqueueAt, cancellationToken);
    
    /// <summary><inheritdoc cref="SaveAndEnqueueAsync{T}(DbContext, IMessageQueue{T}, IEnumerable{T}, DateTime?, CancellationToken)"/></summary>
    public static Task<int> SaveAndEnqueueAsync<T>(this DbContext callerContext, IMessageQueue<T> queue, T item, TimeSpan visibilityWindow, CancellationToken cancellationToken = default) where T : class 
        => callerContext.SaveAndEnqueueAsync(queue, [item], DateTime.UtcNow.Add(visibilityWindow), cancellationToken);

    /// <summary><inheritdoc cref="SaveAndEnqueueAsync{T}(DbContext, IMessageQueue{T}, IEnumerable{T}, DateTime?, CancellationToken)"/></summary>
    public static Task<int> SaveAndEnqueueAsync<T>(this DbContext callerContext, IMessageQueue<T> queue, IEnumerable<T> items, TimeSpan visibilityWindow, CancellationToken cancellationToken = default) where T : class
        => callerContext.SaveAndEnqueueAsync(queue, items, DateTime.UtcNow.Add(visibilityWindow), cancellationToken);

    /// <summary>
    /// Saves pending changes on the caller's - integrator's - dbContext in a single transaction with the specified event is.
    /// <b>If the caller has an opened transaction already, it will be used, so the caller is responsible for commiting.</b>.
    /// </summary>
    /// <returns>The number of affected entities in the <b>caller's</b> db context.</returns>
    public static async Task<int> SaveAndEnqueueAsync<T>(
        this DbContext callerContext,
        IMessageQueue<T> queue,
        IEnumerable<T> items,
        DateTime? enqueueAt = null,
        CancellationToken cancellationToken = default
    ) where T : class {
        ArgumentNullException.ThrowIfNull(callerContext);
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(items);
        
        if (queue is not MessageQueueRelational<T> relationalQueue) {
            throw new NotSupportedException("Outbox requires the relational queue store, using UseStoreRelational() call.");
        }

        var date = enqueueAt ?? DateTime.UtcNow;
        if (callerContext.Database.CurrentTransaction is not null) {
            // Integrator owns the transaction, so just add to it
            return await SaveAndInsert(callerContext, relationalQueue, items, date, cancellationToken);
        }

        var strategy = callerContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            await using var transaction = await callerContext.Database.BeginTransactionAsync(cancellationToken);
            var affected = await SaveAndInsert(callerContext, relationalQueue, items, date, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        });
    }
    
    /// <summary><inheritdoc cref="SaveAndEnqueueAsync{T}(DbContext, IMessageQueue{T}, IEnumerable{T}, DateTime?, CancellationToken)"/></summary>
    public static async Task<int> SaveAndEnqueueAsync(
        this DbContext callerContext,
        Action<OutboxBatch> configureBatch,
        DateTime? enqueueAt = null,
        CancellationToken cancellationToken = default
    ) {
        ArgumentNullException.ThrowIfNull(callerContext);
        ArgumentNullException.ThrowIfNull(configureBatch);

        var batch = new OutboxBatch();
        configureBatch(batch);
        var date = enqueueAt ?? DateTime.UtcNow;

        if (callerContext.Database.CurrentTransaction is not null) {
            // Integrator owns the transaction, so just add to it
            return await SaveAndInsert(callerContext, batch, date, cancellationToken);
        }

        var strategy = callerContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () => {
            await using var transaction = await callerContext.Database.BeginTransactionAsync(cancellationToken);
            var affected = await SaveAndInsert(callerContext, batch, date, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return affected;
        });
    }

    private static async Task<int> SaveAndInsert(
        DbContext callerContext,
        OutboxBatch batch,
        DateTime enqueueAt,
        CancellationToken cancellationToken
    ) {
        var affected = await callerContext.SaveChangesAsync(cancellationToken);
        await batch.ExecuteAsync(callerContext.Database, enqueueAt);
        return affected;
    }

    private static async Task<int> SaveAndInsert<T>(
        DbContext callerContext,
        MessageQueueRelational<T> queue,
        IEnumerable<T> items,
        DateTime enqueueAt,
        CancellationToken cancellationToken
    ) where T : class {
        var affected = await callerContext.SaveChangesAsync(cancellationToken);
        await queue.EnqueueOn(callerContext.Database, items.Select(payload => new QMessage<T> {
            Id = Guid.NewGuid().ToString(),
            Date = enqueueAt,
            Value = payload,
            IsNew = true
        }).ToList());
        
        return affected;
    }
}

/// <summary>Batch of multiple events produced that can be enqueued on multiple queues.</summary>
public sealed class OutboxBatch
{
    private readonly List<Func<DatabaseFacade, DateTime, Task>> _enqueueOnTasks = [];

    /// <summary>Adds a single item to the batch.</summary>
    public OutboxBatch Add<T>(IMessageQueue<T> queue, T item, DateTime? enqueueAt = null) where T : class
        => Add(queue, [item], enqueueAt);

    /// <summary>Adds multiple same type items to the batch.</summary>
    public OutboxBatch Add<T>(IMessageQueue<T> queue, IEnumerable<T> items, DateTime? enqueueAt = null) where T : class {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(items);
        if (queue is not MessageQueueRelational<T> relationalQueue) {
            throw new NotSupportedException("Outbox requires the relational queue store, using UseStoreRelational() call.");
        }
        
        var payloads = items.ToList();
        _enqueueOnTasks.Add((database, date) => relationalQueue.EnqueueOn(database, payloads.Select(payload => new QMessage<T> {
            Id = Guid.NewGuid().ToString(),
            Date = enqueueAt ?? date,
            Value = payload,
            IsNew = true
        }).ToList()));
        
        return this;
    }

    internal async Task ExecuteAsync(DatabaseFacade database, DateTime defaultDate) {
        foreach (var write in _enqueueOnTasks) {
            await write(database, defaultDate);
        }
    }
}