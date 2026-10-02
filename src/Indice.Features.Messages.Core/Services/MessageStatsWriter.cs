using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Models;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StatKey = (Indice.Features.Messages.Core.Models.MessageStatGranularity Granularity, System.DateOnly PeriodStart, Indice.Features.Messages.Core.Models.MessageChannelKind Channel, System.Guid CampaignTypeId);

namespace Indice.Features.Messages.Core.Services;

/// <summary>Writes the send statistics. Every write follows the same steps: lock, read, upsert, unlock.</summary>
/// <remarks>
/// The lock is the <see cref="ILockManager"/> of the host. With a lock that does not really exclude other writers, two writers can read the same value
/// and one increment is lost. A write is never applied twice.
/// </remarks>
public class MessageStatsWriter(IServiceScopeFactory scopeFactory, ILogger<MessageStatsWriter> logger)
{
    /// <summary>The name of the lock that guards every write to the send statistics.</summary>
    public const string LockName = "messaging-stats";
    // Kept below the default lease of the lock (30 seconds), so that a stalled write fails before the lease can expire under it.
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    /// <summary>The number of times to try again when the lock is held by another writer.</summary>
    public int LockRetryCount { get; set; } = 3;
    /// <summary>The time to wait between two attempts to acquire the lock, in seconds.</summary>
    public int LockRetryAfterInSeconds { get; set; } = 1;

    /// <summary>Adds successful sends to the day rows and the month rows.</summary>
    /// <param name="counts">The successful sends to add.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>False when the lock was not acquired. Nothing was written in that case.</returns>
    public Task<bool> AddAsync(IReadOnlyCollection<MessageStatCount> counts, CancellationToken cancellationToken = default) {
        if (counts.Count == 0) {
            return Task.FromResult(true);
        }
        return WriteAsync(async (dbContext, now) => {
            var days = counts.Select(x => x.Day).Distinct().ToList();
            var months = days.Select(MessageStatsEligibility.MonthOf).Distinct().ToList();
            var rows = await dbContext.MessageStats
                .Where(x => (x.Granularity == MessageStatGranularity.Day && days.Contains(x.PeriodStart)) ||
                            (x.Granularity == MessageStatGranularity.Month && months.Contains(x.PeriodStart)))
                .ToListAsync(cancellationToken);
            var index = rows.ToDictionary(KeyOf);
            foreach (var count in counts) {
                Increment(dbContext, index, (MessageStatGranularity.Day, count.Day, count.Channel, count.CampaignTypeId), count, now);
                Increment(dbContext, index, (MessageStatGranularity.Month, MessageStatsEligibility.MonthOf(count.Day), count.Channel, count.CampaignTypeId), count, now);
            }
        }, cancellationToken);
    }

    /// <summary>Replaces the rows of a day with a recount and rebuilds the month rows of that day as the sum of the month's day rows.</summary>
    /// <param name="day">The UTC day.</param>
    /// <param name="counts">The recounted successful sends of the day. A row of the day that is not in the recount is deleted.</param>
    /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
    /// <returns>The rows that were changed, or null when the lock was not acquired. Nothing was written in that case.</returns>
    public async Task<IReadOnlyList<MessageStatDrift>?> CorrectDayAsync(DateOnly day, IReadOnlyCollection<MessageStatCount> counts, CancellationToken cancellationToken = default) {
        var drifts = new List<MessageStatDrift>();
        var written = await WriteAsync(async (dbContext, now) => {
            var month = MessageStatsEligibility.MonthOf(day);
            var nextMonth = month.AddMonths(1);
            // The month row and every day row of the month.
            var rows = await dbContext.MessageStats.Where(x => x.PeriodStart >= month && x.PeriodStart < nextMonth).ToListAsync(cancellationToken);
            var index = rows.ToDictionary(KeyOf);
            var recounted = new HashSet<StatKey>();
            foreach (var count in counts) {
                StatKey key = (MessageStatGranularity.Day, day, count.Channel, count.CampaignTypeId);
                recounted.Add(key);
                Set(dbContext, index, drifts, key, count.Count, count.CampaignTypeName, now);
            }
            foreach (var row in rows.Where(x => x.Granularity == MessageStatGranularity.Day && x.PeriodStart == day && !recounted.Contains(KeyOf(x)))) {
                Set(dbContext, index, drifts, KeyOf(row), 0, null, now);
            }
            var sums = index.Values
                .Where(x => x.Granularity == MessageStatGranularity.Day)
                .GroupBy(x => (x.Channel, x.CampaignTypeId))
                .Select(x => new { x.Key.Channel, x.Key.CampaignTypeId, x.First().CampaignTypeName, Sends = x.Sum(row => row.SuccessfulSends) })
                .ToList();
            var summed = new HashSet<StatKey>();
            foreach (var sum in sums) {
                StatKey key = (MessageStatGranularity.Month, month, sum.Channel, sum.CampaignTypeId);
                summed.Add(key);
                Set(dbContext, index, drifts, key, sum.Sends, sum.CampaignTypeName, now);
            }
            foreach (var row in rows.Where(x => x.Granularity == MessageStatGranularity.Month && !summed.Contains(KeyOf(x)))) {
                Set(dbContext, index, drifts, KeyOf(row), 0, null, now);
            }
        }, cancellationToken);
        return written ? drifts : null;
    }

    private async Task<bool> WriteAsync(Func<CampaignsDbContext, DateTimeOffset, Task> change, CancellationToken cancellationToken) {
        await using var scope = scopeFactory.CreateAsyncScope();
        // The lock manager comes from the scope, because some implementations are registered as scoped.
        var lockManager = scope.ServiceProvider.GetRequiredService<ILockManager>();
        var lockResult = await lockManager.TryAcquireLockWithRetryPolicy(LockName, LockRetryCount, LockRetryAfterInSeconds, cancellationToken);
        if (!lockResult.Ok) {
            return false;
        }
        try {
            var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
            dbContext.Database.SetCommandTimeout(CommandTimeout);
            await change(dbContext, DateTimeOffset.UtcNow);
            await dbContext.SaveChangesAsync(cancellationToken);
        } finally {
            await ReleaseAsync(lockResult.Lock!);
        }
        return true;
    }

    // A release that fails must not turn a write that was saved into a failed one. The caller would write it again.
    private async Task ReleaseAsync(ILockLease lockLease) {
        try {
            await lockLease.DisposeAsync();
        } catch (Exception exception) {
            logger.LogWarning(exception, "The lock '{LockName}' could not be released. It is free again when its lease expires.", LockName);
        }
    }

    private static StatKey KeyOf(DbMessageStat row) => (row.Granularity, row.PeriodStart, row.Channel, row.CampaignTypeId);

    private static DbMessageStat Create(StatKey key, string? campaignTypeName, int successfulSends, DateTimeOffset now) => new() {
        Granularity = key.Granularity,
        PeriodStart = key.PeriodStart,
        Channel = key.Channel,
        CampaignTypeId = key.CampaignTypeId,
        CampaignTypeName = campaignTypeName,
        SuccessfulSends = successfulSends,
        UpdatedAt = now
    };

    private static void Increment(CampaignsDbContext dbContext, Dictionary<StatKey, DbMessageStat> index, StatKey key, MessageStatCount count, DateTimeOffset now) {
        if (index.TryGetValue(key, out var row)) {
            row.SuccessfulSends += count.Count;
            row.UpdatedAt = now;
            return;
        }
        row = Create(key, count.CampaignTypeName, count.Count, now);
        dbContext.MessageStats.Add(row);
        index.Add(key, row);
    }

    private static void Set(CampaignsDbContext dbContext, Dictionary<StatKey, DbMessageStat> index, List<MessageStatDrift> drifts, StatKey key, int successfulSends, string? campaignTypeName, DateTimeOffset now) {
        index.TryGetValue(key, out var row);
        var stored = row?.SuccessfulSends ?? 0;
        if (successfulSends <= 0) {
            if (row is null) {
                return;
            }
            dbContext.MessageStats.Remove(row);
            index.Remove(key);
        } else if (row is null) {
            row = Create(key, campaignTypeName, successfulSends, now);
            dbContext.MessageStats.Add(row);
            index.Add(key, row);
        } else if (row.SuccessfulSends != successfulSends) {
            row.SuccessfulSends = successfulSends;
            row.UpdatedAt = now;
        } else {
            return;
        }
        drifts.Add(new MessageStatDrift {
            Granularity = key.Granularity,
            PeriodStart = key.PeriodStart,
            Channel = key.Channel,
            CampaignTypeId = key.CampaignTypeId,
            Stored = stored,
            Recounted = Math.Max(successfulSends, 0)
        });
    }
}
