using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
using Indice.Features.Messages.Core.Events;
using Indice.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// Persists messaging provider webhook events with database-enforced idempotency.
/// </summary>
/// <param name="dbContext">The Messages database context.</param>
/// <param name="logger">The logger used to record duplicate events.</param>
public sealed class MessagesWebhookHandler(CampaignsDbContext dbContext, ILogger<MessagesWebhookHandler> logger) : IWebhookHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(List<WebhookEvent> events, CancellationToken cancellationToken) {
        foreach (var webhookEvent in events) {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(webhookEvent.MessageId)) {
                logger.LogWarning("Message with ID {MessageId} not found for webhook event {Provider}/{EventId}", webhookEvent.MessageId, webhookEvent.Provider, webhookEvent.EventId);
            }
            var createdEvent = await dbContext.MessageEvents.FirstOrDefaultAsync(x => x.ReceiptId == webhookEvent.MessageId && x.Type == nameof(MessageEventType.Sent), cancellationToken);
            if (createdEvent is null) {
                logger.LogWarning("Message with ID {MessageId} not found for webhook event {Provider}/{EventId}", webhookEvent.MessageId, webhookEvent.Provider, webhookEvent.EventId);
                continue;
            }
            var entity = new DbMessageEvent {
                Id = Guid.NewGuid(),
                MessageId = createdEvent.MessageId,
                CampaignId = createdEvent.CampaignId,
                ContactId = createdEvent.ContactId,
                Title = createdEvent.Title,
                Channel = createdEvent.Channel,
                CreatedOn = DateTimeOffset.UtcNow,
                Success = true,
                Type = webhookEvent.EventType
            };
            dbContext.MessageEvents.Add(entity);
        }
        try {
            await dbContext.SaveChangesAsync(cancellationToken);
        } catch (DbUpdateException ex) {
            logger.LogError(ex, "Error saving message events to the database");
        } catch (OperationCanceledException ex) {
            logger.LogWarning(ex, "Saving message events was canceled.");
            throw;
        }
    }
}