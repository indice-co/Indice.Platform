namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>
/// Interface for database cleanup services.
/// </summary>
public interface IDatabaseCleanUpService
{
    /// <summary>
    /// Cleans up the campaigns with inbox and their related data from the database.
    /// </summary>
    Task CleanUpCampaignsWithInboxAsync();

    /// <summary>
    /// Cleans up the campaigns without inbox and their related data from the database.
    /// </summary>
    Task CleanUpCampaignsWithoutInboxAsync();

}
