using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Open.ChannelExtensions;

namespace Indice.Features.Messages.Core.Services;
/// <summary>Background service that handles the processing of campaign events.</summary>
public class CampaignEventHandler(
    CampaignEventQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<CampaignEventHandler> logger) : BackgroundService
{
    /// <summary>Batch size for dequeuing events from the queue.</summary>
    public int DequeueBatchSize { get; set; } = 10;
    /// <summary>Timeout for dequeuing events from the queue in milliseconds.</summary>
    public long DequeueTimeoutInMilliseconds { get; set; } = 1000;

    ///<inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        var events = queue.Reader
                 .Pipe(LastEvent => LastEvent, cancellationToken: stoppingToken)
                 .Filter(logEntry => logEntry is not null)
                 .Batch(DequeueBatchSize)
                 .WithTimeout(DequeueTimeoutInMilliseconds)
                 .ReadAllAsync(stoppingToken);

        await foreach (var lastActivityBatch in events) {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
            try {
                await UpsertBatchAsync(lastActivityBatch, db, stoppingToken);
            } catch (Exception ex) {
                logger.LogError(ex, "Failed to upsert CampaignEvents");
            }
        }
    }

    /// <summary>Upserts a batch of campaign events into the database.</summary>
    private static async Task UpsertBatchAsync(List<CampaignEvent> lastActivityBatch, CampaignsDbContext db, CancellationToken stoppingToken) {
        var entries = lastActivityBatch.Select(activity => new DbCampaignEvent() {
            Type = activity.Type,
            CampaignId = activity.CampaignId,
            ContactId = activity.ContactId,
            CreatedOn = activity.CreatedOn
        });
        await db.CampaignEvent.AddRangeAsync(entries, stoppingToken);
        await db.SaveChangesAsync(stoppingToken);
    }
}
