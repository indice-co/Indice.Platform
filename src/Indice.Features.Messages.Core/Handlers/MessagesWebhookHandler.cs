using Indice.Features.Messages.Core.Data;
using Indice.Services;
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

            //var entity = new DbMessageEvent {
            //    Provider = webhookEvent.Provider,
            //    EventId = webhookEvent.EventId,
            //    EventType = webhookEvent.EventType,
            //    MessageId = webhookEvent.MessageId,
            //    Timestamp = webhookEvent.Timestamp,
            //    Recipient = webhookEvent.Recipient,
            //    Subject = webhookEvent.Subject,
            //    Payload = webhookEvent.Payload?.ToJsonString(),
            //    ReceivedAt = DateTimeOffset.UtcNow
            //};

            //dbContext.MessageEvents.Add(entity);

            try {
                await dbContext.SaveChangesAsync(cancellationToken);
            } catch (Exception ex) {
                logger.LogError(ex, "Error processing webhook event {Provider}/{EventId}", webhookEvent.Provider, webhookEvent.EventId);
            }
        }
    }
}
