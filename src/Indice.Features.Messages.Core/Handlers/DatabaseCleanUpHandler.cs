using Indice.Features.Messages.Core.Events;
using Indice.Features.Messages.Core.Services;

namespace Indice.Features.Messages.Core.Handlers;

/// <summary>
/// 
/// </summary>
public sealed class DatabaseCleanUpHandler : ICampaignJobHandler<DatabaseCleanUpTimerEvent>
{
    private readonly DatabaseCleanUpService _cleanUpService;

    /// <summary>
    /// The contructor for our handler.
    /// </summary>
    /// <param name="cleanUpService"></param>
    public DatabaseCleanUpHandler(DatabaseCleanUpService cleanUpService) {
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
