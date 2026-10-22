using Indice.Hosting.Data.Models;
using Indice.Hosting.Models;
using Indice.Hosting.UI.Models;

namespace Indice.Hosting.UI.Services;

/// <summary>
/// The single source of truth for the status of a queue message. The first match wins: Poison, Dequeued, Scheduled, Retrying, Enqueued.
/// <see cref="Classify"/> evaluates it in memory and <see cref="WhereStatus"/> as SQL predicates; both must agree.
/// </summary>
public static class QueueMessageStatusRules
{
    /// <summary>Removes the <see cref="HostingConventions.PoisonSuffix"/> from a physical queue name.</summary>
    /// <param name="queueName">The physical queue name.</param>
    public static string GetLogicalQueueName(string queueName) =>
        queueName.EndsWith(HostingConventions.PoisonSuffix, StringComparison.Ordinal) ? queueName[..^HostingConventions.PoisonSuffix.Length] : queueName;

    /// <summary>Classifies a message.</summary>
    /// <param name="queueName">The physical queue name.</param>
    /// <param name="state">The stored state.</param>
    /// <param name="date">The date the message becomes visible (UTC).</param>
    /// <param name="dequeueCount">The number of failed attempts.</param>
    /// <param name="utcNow">The current time (UTC).</param>
    public static QueueMessageStatus Classify(string queueName, QMessageState state, DateTime date, int dequeueCount, DateTime utcNow) {
        if (queueName.EndsWith(HostingConventions.PoisonSuffix, StringComparison.Ordinal) || state == QMessageState.Poison) {
            return QueueMessageStatus.Poison;
        }
        
        if (state == QMessageState.Dequeued) {
            return QueueMessageStatus.Dequeued;
        }
        
        if (date > utcNow) {
            return QueueMessageStatus.Scheduled;
        }
        
        return dequeueCount > 0 ? QueueMessageStatus.Retrying : QueueMessageStatus.Enqueued;
    }

    /// <summary>Filters the messages of the given <paramref name="status"/>, with the same rule as <see cref="Classify"/>.</summary>
    /// <param name="query">The messages.</param>
    /// <param name="status">The status to keep.</param>
    /// <param name="utcNow">The current time (UTC).</param>
    public static IQueryable<DbQMessage> WhereStatus(this IQueryable<DbQMessage> query, QueueMessageStatus status, DateTime utcNow) => status switch {
        // EF translates only the EndsWith(string) overload, so no StringComparison here.
        QueueMessageStatus.Poison => query.Where(x => x.QueueName.EndsWith(HostingConventions.PoisonSuffix) || x.State == QMessageState.Poison),
        QueueMessageStatus.Dequeued => query.Where(x => !(x.QueueName.EndsWith(HostingConventions.PoisonSuffix) || x.State == QMessageState.Poison)
                                                     && x.State == QMessageState.Dequeued),
        QueueMessageStatus.Scheduled => query.Where(x => !(x.QueueName.EndsWith(HostingConventions.PoisonSuffix) || x.State == QMessageState.Poison)
                                                      && x.State != QMessageState.Dequeued
                                                      && x.Date > utcNow),
        QueueMessageStatus.Retrying => query.Where(x => !(x.QueueName.EndsWith(HostingConventions.PoisonSuffix) || x.State == QMessageState.Poison)
                                                     && x.State != QMessageState.Dequeued
                                                     && x.Date <= utcNow
                                                     && x.DequeueCount > 0),
        QueueMessageStatus.Enqueued => query.Where(x => !(x.QueueName.EndsWith(HostingConventions.PoisonSuffix) || x.State == QMessageState.Poison)
                                                     && x.State != QMessageState.Dequeued
                                                     && x.Date <= utcNow
                                                     && x.DequeueCount == 0),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
