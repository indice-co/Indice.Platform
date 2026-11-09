using Indice.Hosting.Data;
using Indice.Hosting.Services;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Microsoft.EntityFrameworkCore;

/// <summary>OutboxDbContextExtensions</summary>
public static class OutboxDbContextExtensions
{
    /// <summary><inheritdoc cref="AddAndEnqueue{TEntity, TEvent}(ITaskDbContext,TEntity, TEvent, DateTime?)"/></summary>
    public static void AddAndEnqueue<TEntity, TEvent>(this ITaskDbContext dbContext, TEntity entity, TEvent @event, TimeSpan visibilityWindow)
        where TEntity : class where TEvent : class
        => dbContext.AddAndEnqueue(entity, @event, DateTime.UtcNow.Add(visibilityWindow));

    /// <summary>
    /// Adds the entity and the event to the change tracker.
    /// The integrator is responsible for calling <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="dbContext">The integrator's context.</param>
    /// <param name="entity">The business entity to add.</param>
    /// <param name="event">The event to publish.</param>
    /// <param name="enqueueAt">When the message becomes visible to consumers. Defaults to now.</param>
    public static void AddAndEnqueue<TEntity, TEvent>(this ITaskDbContext dbContext, TEntity entity, TEvent @event, DateTime? enqueueAt = null) 
        where TEntity : class where TEvent : class {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entity);
        dbContext.Add(entity);
        dbContext.EnqueueRange([@event], enqueueAt);
    }

    /// <summary><inheritdoc cref="AddAndEnqueueRange{TEntity, TEvent}(ITaskDbContext,IEnumerable{TEntity}, IEnumerable{TEvent}, DateTime?)" path="/summary"/></summary>
    public static void AddAndEnqueueRange<TEntity, TEvent>(this ITaskDbContext dbContext, IEnumerable<TEntity> entities, IEnumerable<TEvent> events, TimeSpan visibilityWindow)
        where TEntity : class where TEvent : class
        => dbContext.AddAndEnqueueRange(entities, events, DateTime.UtcNow.Add(visibilityWindow));

    /// <summary>
    /// Adds the entities and the events to the change tracker.
    /// The integrator is responsible for calling <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// </summary>
    /// <remarks>
    /// Every event is published to the queue of <typeparamref name="TEvent"/>. You can chain multiple calls to publish to another queue in the same transaction.
    /// </remarks>
    /// <param name="dbContext">The integrator's context.</param>
    /// <param name="entities">The business entities to add.</param>
    /// <param name="events">The events to publish.</param>
    /// <param name="enqueueAt">When the messages become visible to consumers. Defaults to now.</param>
    public static void AddAndEnqueueRange<TEntity, TEvent>(this ITaskDbContext dbContext, IEnumerable<TEntity> entities, IEnumerable<TEvent> events, DateTime? enqueueAt = null)
        where TEntity : class where TEvent : class {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(entities);
        dbContext.AddRange(entities);
        dbContext.EnqueueRange(events, enqueueAt);
    }

    /// <summary><inheritdoc cref="EnqueueRange{TEvent}(ITaskDbContext,IEnumerable{TEvent}, DateTime?)" path="/summary"/></summary>
    public static void Enqueue<TEvent>(this ITaskDbContext dbContext, TEvent @event, DateTime? enqueueAt = null) where TEvent : class
        => dbContext.EnqueueRange([@event], enqueueAt);

    /// <summary><inheritdoc cref="EnqueueRange{TEvent}(ITaskDbContext,IEnumerable{TEvent}, DateTime?)" path="/summary"/></summary>
    public static void Enqueue<TEvent>(this ITaskDbContext dbContext, TEvent @event, TimeSpan visibilityWindow) where TEvent : class
        => dbContext.EnqueueRange([@event], DateTime.UtcNow.Add(visibilityWindow));

    /// <summary><inheritdoc cref="EnqueueRange{TEvent}(ITaskDbContext,IEnumerable{TEvent}, DateTime?)" path="/summary"/></summary>
    public static void EnqueueRange<TEvent>(this ITaskDbContext dbContext, IEnumerable<TEvent> events, TimeSpan visibilityWindow) where TEvent : class
        => dbContext.EnqueueRange(events, DateTime.UtcNow.Add(visibilityWindow));

    /// <summary>
    /// Enqueues a range of events.
    /// The integrator is responsible for calling <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// </summary>
    /// <param name="dbContext">The integrator's context.</param>
    /// <param name="events">The events to publish.</param>
    /// <param name="enqueueAt">When the messages become visible to consumers. Defaults to now.</param>
    public static void EnqueueRange<TEvent>(this ITaskDbContext dbContext, IEnumerable<TEvent> events, DateTime? enqueueAt = null) where TEvent : class {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(events);
        
        var queue = dbContext.Database.GetService<IMessageQueue<TEvent>>();
        if (queue is not MessageQueueRelational<TEvent> relationalQueue) {
            throw new InvalidOperationException("Outbox requires the relational queue store, use UseStoreRelational() call.");
        }
        
        var date = enqueueAt ?? DateTime.UtcNow;
        foreach (var @event in events) {
            dbContext.Queue.Add(relationalQueue.CreateMessage(@event, date));
        }
    }
}
