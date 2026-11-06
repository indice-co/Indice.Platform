using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Data.Models;
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
            if (Guid.TryParse(webhookEvent.MessageId, out var messageId)) {
                var message = await dbContext.Messages.FirstOrDefaultAsync(x => x.Id == messageId, cancellationToken);
                if (message is null) {
                    logger.LogWarning("Message with ID {MessageId} not found for webhook event {Provider}/{EventId}", messageId, webhookEvent.Provider, webhookEvent.EventId);
                    continue;
                }
                var entity = new DbMessageEvent {
                    Id = Guid.NewGuid(),
                    MessageId = messageId,
                    CampaignId = message.CampaignId,
                    ContactId = message.ContactId!.Value,
                    CreatedOn = DateTimeOffset.UtcNow,
                    Success = true,
                    Channel = Models.MessageChannelKind.Email.ToString(),
                    Title = webhookEvent.Subject ?? message.GetContentTitle(Models.MessageChannelKind.Email),
                    Type = $"Webhook-{webhookEvent.Provider}"
                };
                /*
                    string MessageId
                    DateTimeOffset Timestamp
                    string EventId
                    string Recipient
                    string? Subject
                    JsonNode? Payload
                 */
                dbContext.MessageEvents.Add(entity);
            }
        }
        try {
            await dbContext.SaveChangesAsync(cancellationToken);
        } catch (Exception ex) {
            logger.LogError(ex, "Error saving message events to the database");
        }
    }
}
