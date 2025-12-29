using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services;
using Indice.Features.Messages.Core.Services.Abstractions;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// Handles database cleanup timer events by triggering the cleanup of old campaign data.
/// </summary>
public sealed class DatabaseCleanUpHandler : ICampaignJobHandler<DatabaseCleanUpTimerEvent>
{
    private readonly IDatabaseCleanUpService _cleanUpService;

    /// <summary>
    /// The constructor for our handler.
    /// </summary>
    /// <param name="cleanUpService"></param>
    public DatabaseCleanUpHandler(IDatabaseCleanUpService cleanUpService) {
        _cleanUpService = cleanUpService ?? throw new ArgumentNullException(nameof(cleanUpService));
    }

    /// <summary>
    /// Calls the database clean up process.
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public async Task Process(DatabaseCleanUpTimerEvent @event) {
        await _cleanUpService.CleanUpCampaignsWithInboxAsync();
        await _cleanUpService.CleanUpCampaignsWithoutInboxAsync();
    }
}
