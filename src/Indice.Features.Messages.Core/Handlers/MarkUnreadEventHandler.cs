using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Features.Messages.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>Job handler for <see cref="MarkMessagesUnreadEvent"/>.</summary>
public class MarkUnreadEventHandler : ICampaignJobHandler<MarkMessagesUnreadEvent>
{
    /// <summary>Creates a new instance of <see cref="MarkUnreadEventHandler"/>.</summary>
    /// <param name="messageService">Push notification service abstraction in order to support different providers.</param>
    /// <param name="logger">Logging</param>
    /// <exception cref="ArgumentNullException"></exception>
    public MarkUnreadEventHandler(IMessageService messageService, ILogger<MarkUnreadEventHandler> logger) {
        _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private readonly IMessageService _messageService;
    private readonly ILogger<MarkUnreadEventHandler> _logger;

    /// <summary>Sends a push notification to a single user.</summary>
    /// <param name="event">The event model used when sending an email.</param>
    public async Task Process(MarkMessagesUnreadEvent @event) {
        @event.Filter ??= new MessagesFilter() { IsRead = true };
        if (@event.Filter.IsRead == false) {
            _logger.LogWarning("The filter for marking messages as unread should have IsRead set to true. Skipping processing for user {UserCode}.", @event.UserCode);
            return;
        }
        @event.Filter.IsRead ??= true; // Ensure IsRead is set to true for filtering.

        var options = new ListOptions() { Page = 1, Size = 100, Search = @event.SearchTerm };
        while (true) {
            var messages = await _messageService.GetList(@event.UserCode, ListOptions.Create(options, @event.Filter));
            if (messages == null || messages.Count == 0) return; // No messages to process.
            foreach (var messageId in messages.Items.Select(x => x.Id)) {
                try {
                    await _messageService.MarkAsUnread(messageId, @event.UserCode);
                } catch (BusinessException be) {
                    _logger.LogWarning(be, "Could not mark message {Messageid} as unread.", messageId);
                } catch (DbUpdateException dbEx) {
                    _logger.LogError(dbEx, "Database update failed while marking {Messageid} as unread.", messageId);
                } catch (Exception ex) {
                    _logger.LogError(ex, "An unexpected error occurred while marking {Messageid} as unread.", messageId);
                }
            }
        }
    }
}
