using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;
/// <summary>Background service that sums the successful sends of the <see cref="MessageStatsQueue"/> and adds them to the send statistics.</summary>
public class MessageStatsHostedService(
    MessageStatsQueue queue,
    MessageStatsWriter writer,
    IServiceScopeFactory scopeFactory,
    IOptions<AnalyticsOptions> analyticsOptions,
    ILogger<MessageStatsHostedService> logger) : BackgroundService
{
    private const int CampaignLookupBatchSize = 500;
    private static readonly TimeSpan ShutdownFlushTimeout = TimeSpan.FromSeconds(10);
    // The sums that are not written yet. A flush that fails leaves them here and the next flush writes them together with the new ones.
    private readonly Dictionary<(DateOnly Day, MessageChannelKind Channel, Guid CampaignId), int> _pending = [];
    private int _failedFlushes;
    private long _reportedDrops;

    ///<inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var options = analyticsOptions.Value;
        if (!options.Enabled || !options.Stats.Enabled) {
            return;
        }
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await ReadWindowAsync(options.Stats, stoppingToken);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                break;
            }
            await FlushAsync(stoppingToken);
        }
        // The host is stopping. Write what is left, within a time limit.
        while (queue.Reader.TryRead(out var delta)) {
            Add(delta);
        }
        using var shutdownSource = new CancellationTokenSource(ShutdownFlushTimeout);
        await FlushAsync(shutdownSource.Token);
    }

    /// <summary>Reads from the queue into the pending sums, until the flush interval has passed or the batch size is reached.</summary>
    private async Task ReadWindowAsync(MessageStatsOptions options, CancellationToken stoppingToken) {
        using var windowSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        // With nothing pending the window opens when the first item arrives. With sums carried from a failed flush it opens now, so that they are written again even if no new item arrives.
        var windowOpen = _pending.Count > 0;
        if (windowOpen) {
            windowSource.CancelAfter(options.FlushInterval);
        }
        var count = 0;
        try {
            while (count < options.FlushBatchSize && await queue.Reader.WaitToReadAsync(windowSource.Token)) {
                if (!windowOpen) {
                    windowSource.CancelAfter(options.FlushInterval);
                    windowOpen = true;
                }
                while (count < options.FlushBatchSize && queue.Reader.TryRead(out var delta)) {
                    Add(delta);
                    count++;
                }
            }
        } catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested) {
            // The flush interval has passed.
        }
    }

    /// <summary>Adds a successful send to the pending sums.</summary>
    internal void Add(MessageStatDelta delta) {
        var key = (delta.Day, delta.Channel, delta.CampaignId);
        _pending[key] = _pending.GetValueOrDefault(key) + 1;
    }

    /// <summary>Writes the pending sums to the send statistics. On failure the sums are kept for the next flush.</summary>
    /// <returns>False when the sums were not written.</returns>
    internal async Task<bool> FlushAsync(CancellationToken cancellationToken) {
        ReportDrops();
        if (_pending.Count == 0) {
            return true;
        }
        try {
            // The campaign lookup runs before the lock is taken.
            var counts = await ResolveAsync(cancellationToken);
            if (await writer.AddAsync(counts, cancellationToken)) {
                _pending.Clear();
                _failedFlushes = 0;
                return true;
            }
            logger.LogWarning("The lock '{LockName}' was not acquired. The send statistics are carried to the next flush.", MessageStatsWriter.LockName);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // The host is stopping. The sums stay pending for the last flush.
            return false;
        } catch (Exception exception) {
            logger.LogError(exception, "The send statistics could not be written. They are carried to the next flush.");
        }
        _failedFlushes++;
        if (_failedFlushes >= analyticsOptions.Value.Stats.MaxCarriedFlushes) {
            logger.LogError("Dropped {Sends} successful sends from the send statistics after {Flushes} failed flushes in a row. The verification job corrects the days when they are complete.", _pending.Values.Sum(), _failedFlushes);
            _pending.Clear();
            _failedFlushes = 0;
        }
        return false;
    }

    /// <summary>Converts the pending sums, kept per campaign, to sums per campaign type. Global campaigns are left out.</summary>
    private async Task<List<MessageStatCount>> ResolveAsync(CancellationToken cancellationToken) {
        var campaigns = new Dictionary<Guid, (bool IsGlobal, Guid? TypeId, string? TypeName)>();
        using (var scope = scopeFactory.CreateScope()) {
            var dbContext = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
            foreach (var campaignIds in _pending.Keys.Select(x => x.CampaignId).Distinct().Chunk(CampaignLookupBatchSize)) {
                var batch = await dbContext.Campaigns
                    .AsNoTracking()
                    .Where(x => campaignIds.Contains(x.Id))
                    .Select(x => new { x.Id, x.IsGlobal, x.TypeId, TypeName = (string?)x.Type.Name })
                    .ToListAsync(cancellationToken);
                foreach (var campaign in batch) {
                    campaigns[campaign.Id] = (campaign.IsGlobal, campaign.TypeId, campaign.TypeName);
                }
            }
        }
        var counts = new Dictionary<(DateOnly Day, MessageChannelKind Channel, Guid CampaignTypeId), MessageStatCount>();
        foreach (var (key, sends) in _pending) {
            // A campaign that has no type, or that cannot be found, is counted as unclassified.
            var campaignTypeId = Guid.Empty;
            string? campaignTypeName = null;
            if (campaigns.TryGetValue(key.CampaignId, out var campaign)) {
                if (campaign.IsGlobal) {
                    continue;
                }
                if (campaign.TypeId.HasValue) {
                    campaignTypeId = campaign.TypeId.Value;
                    campaignTypeName = campaign.TypeName;
                }
            }
            var countKey = (key.Day, key.Channel, campaignTypeId);
            if (!counts.TryGetValue(countKey, out var count)) {
                count = new MessageStatCount {
                    Day = key.Day,
                    Channel = key.Channel,
                    CampaignTypeId = campaignTypeId,
                    CampaignTypeName = campaignTypeName
                };
                counts.Add(countKey, count);
            }
            count.Count += sends;
        }
        return [.. counts.Values];
    }

    private void ReportDrops() {
        var drops = queue.DroppedCount;
        if (drops > _reportedDrops) {
            logger.LogWarning("The send statistics queue was full and dropped {Drops} successful sends. The verification job corrects the days when they are complete.", drops - _reportedDrops);
            _reportedDrops = drops;
        }
    }
}
