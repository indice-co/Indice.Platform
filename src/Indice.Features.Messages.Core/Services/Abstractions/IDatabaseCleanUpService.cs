using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>
/// Interface for database cleanup services.
/// </summary>
public interface IDatabaseCleanUpService
{
    /// <summary>
    /// Cleans up the campaigns with inbox and their related data from the database, based on retention policies.
    /// </summary>
    Task CleanUpCampaignsWithInboxAsync();

    /// <summary>
    /// Cleans up the campaigns without inbox and their related data from the database, based on retention policies.
    /// </summary>
    Task CleanUpCampaignsWithoutInboxAsync();

}
