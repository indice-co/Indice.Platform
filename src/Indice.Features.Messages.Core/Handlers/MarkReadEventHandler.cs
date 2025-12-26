using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="MarkMessagesReadEvent"/>.</summary>
public class MarkReadEventHandler : ICampaignJobHandler<MarkMessagesReadEvent>
{
    /// <summary>Creates a new instance of <see cref="MarkReadEventHandler"/>.</summary>
    /// <param name="messageService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="logger">Logging</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MarkReadEventHandler(IMessageService messageService, ILogger<MarkReadEventHandler> logger) {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private readonly IMessageService _messageService;
    private readonly ILogger<MarkReadEventHandler> _logger;

    /// <summary>Sends a push notification to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(MarkMessagesReadEvent @event) {
        @event.Filter ??= new MessagesFilter() { IsRead = false };
        if (@event.Filter.IsRead == true) {
            _logger.LogWarning("The filter for marking messages as read should have IsRead set to false. Skipping processing for user {UserCode}.", @event.UserCode);
            return;
        }
        @event.Filter.IsRead ??= false; // Ensure IsRead is set to true for filtering.
        var options = new ListOptions() { Page = 1, Size = 100, Search = @event.SearchTerm };
        while (true) {
            var messages = await _messageService.GetList(@event.UserCode, ListOptions.Create(options, @event.Filter));
            if (messages == null || messages.Count == 0) return; // No messages to process.
            foreach (var messageId in messages.Items.Select(x => x.Id)) {
                try {
                    await _messageService.MarkAsRead(messageId, @event.UserCode);
                } catch (BusinessException be) {
                    _logger.LogWarning(be, "Could not mark message {Messageid} as read.", messageId);
                } catch (DbUpdateException dbEx) {
                    _logger.LogError(dbEx, "Database update failed while marking {Messageid} as read.", messageId);
                } catch (Exception ex) {
                    _logger.LogError(ex, "An unexpected error occurred while marking {Messageid} as read.", messageId);
                }
            }
        }
    }

}
