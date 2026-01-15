using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// Handles database cleanup timer events by triggering the cleanup of old campaign data.
/// </summary>
public sealed class MessagingDatabaseCleanUpHandler : ICampaignJobHandler<MessagingDatabaseCleanUpTimerEvent>
{
    private readonly IMessagingDatabaseCleanUpService _cleanUpService;

    /// <summary>
    /// The constructor for our handler.
    /// </summary>
    /// <param name="cleanUpService"></param>
    public MessagingDatabaseCleanUpHandler(IMessagingDatabaseCleanUpService cleanUpService) {
        _cleanUpService = cleanUpService ?? throw new ArgumentNullException(nameof(cleanUpService));
    }

    /// <summary>
    /// Calls the database clean up process.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public async Task Process(MessagingDatabaseCleanUpTimerEvent @event) {
        await _cleanUpService.CleanUpCampaignsWithInboxAsync();
        await _cleanUpService.CleanUpCampaignsWithoutInboxAsync();
    }
}
