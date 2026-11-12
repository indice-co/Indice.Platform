using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// Persists messaging provider webhook events. Duplicate deliveries are accepted, so repeated events (e.g. multiple opens) are all recorded.
/// </summary>
/// <param name="dbContext">The Messages database context.</param>
/// <param name="logger">The logger used to record persistence failures.</param>
public sealed class MessagesWebhookHandler(CampaignsDbContext dbContext, ILogger<MessagesWebhookHandler> logger) : IWebhookHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(
        List<WebhookEvent> events, CancellationToken cancellationToken) {
        var eventIds = events.Where(x => !string.IsNullOrEmpty(x.MessageId)).Select(x => x.MessageId!).Distinct().ToList();
        var existingEvents = await dbContext.MessageEvents
            .AsNoTracking()
            .Where(x => eventIds.Contains(x.ReceiptId!) && x.Type == nameof(MessageEventType.Sent))
            .ToListAsync(cancellationToken);

        foreach (var webhookEvent in events) {
            cancellationToken.ThrowIfCancellationRequested();
            var existingEvent = existingEvents.FirstOrDefault(x => x.ReceiptId == webhookEvent.MessageId && x.Type == nameof(MessageEventType.Sent));
            if (existingEvent is not null) {
                dbContext.MessageEvents.Add(new DbMessageEvent {
                    Id = Guid.NewGuid(),
                    MessageId = existingEvent.MessageId,
                    CampaignId = existingEvent.CampaignId,
                    ContactId = existingEvent.ContactId,
                    Title = existingEvent.Title,
                    Channel = existingEvent.Channel,
                    ReceiptId = webhookEvent.MessageId,
                    CreatedOn = GetCreatedOn(webhookEvent),
                    Success = webhookEvent.Succeeded,
                    Type = webhookEvent.EventType,
                    Recipient = existingEvent.Recipient
                });
                continue;
            }
            dbContext.MessageEvents.Add(new DbMessageEvent {
                Id = Guid.NewGuid(),
                MessageId = null,
                ReceiptId = webhookEvent.MessageId,
                CampaignId = Guid.Empty,
                ContactId = Guid.Empty,
                Orphan = true,
                Title = webhookEvent.Subject ?? webhookEvent.Provider + " " + webhookEvent.EventType,
                Channel = webhookEvent.Channel,
                CreatedOn = GetCreatedOn(webhookEvent),
                Success = webhookEvent.Succeeded,
                Type = webhookEvent.EventType,
                Recipient = webhookEvent.Recipient ?? "unknown"
            });
        }
        try {
            await dbContext.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException ex) {
            logger.LogError(ex, "Error saving message events to the database");
            throw;
        } catch (OperationCanceledException ex) {
            logger.LogWarning(ex, "Saving message events was canceled.");
            throw;
        }
    }

    private static DateTimeOffset GetCreatedOn(WebhookEvent webhookEvent) =>
        webhookEvent.Timestamp == default ? DateTimeOffset.UtcNow : webhookEvent.Timestamp;
}