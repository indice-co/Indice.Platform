
using System.Linq;
using Indice.Features.Messages.Core.Data;
using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Indice.Features.Messages.Core.Services;

/// <inheritdoc/>
public class DatabaseCleanupService : IDatabaseCleanUpService
{
    private readonly DatabaseCleanUpOptions _options;
    private CampaignsDbContext DbContext { get; }

    /// <summary>
    /// Constructs the service.
    /// </summary>
    /// <param name="options">Retention policies for database cleanup.</param>
    /// <param name="dbContext">Database context for accessing campaign data.</param>
    public DatabaseCleanupService(IOptions<DatabaseCleanUpOptions> options, CampaignsDbContext dbContext) {
        _options = options.Value;
        DbContext = dbContext;
    }

    /// <summary>
    /// Cleans up campaigns with an inbox and their related Data.
    /// </summary>
    /// <returns></returns>
    public async Task CleanUpCampaignsWithInboxAsync() {

        //Get all the campaigns with an inbox
        //That are older than the retention period - Max of Created, From
        //Find all the related non cascade fields and delete them
        //Execute delete async on campaigns.

        //Lets mark the affected table/rows
        //Campaigns -> Also deletes Messages by Cascade
        //DLs and their DLContacts -> IF their creator is system aka the campaign
        //The message Events

        //Order of deletion -> Probably DLContacts, DLs, MessageEvents, Campaigns (Messages are cascade deleted)
        //Actually DLContacts also has cascade delete on DLs so we can just delete DLs
        var cutOffDate = DateTimeOffset.UtcNow.AddDays(-_options.CampaignsWithInboxRetentionPeriodInDays);
        var query = DbContext.Campaigns.Where(x => x.MessageChannelKind.HasFlag(MessageChannelKind.Inbox));
        query = query.Where(x => x.CreatedAt <= cutOffDate && 
        (!(x.ActivePeriod != null && x.ActivePeriod.From != null) || x.ActivePeriod.From <= cutOffDate));
        var campaignIds = query.Select(x => x.Id);
        var distributionListIds = query.Select(x => x.DistributionListId);

        //unfortunately - I think that I have to materialize this
        var distributionListsToDelete = await DbContext.DistributionLists.Where(x => x.CreatedBy == "system" && distributionListIds.Contains(x.Id)).Select(x => x.Id).ToListAsync();

        //
        //await DbContext.MessageEvents.Where(x => campaignIds.Contains(x.CampaignId)).ExecuteDeleteAsync();
        //await query.ExecuteDeleteAsync();
        //await DbContext.DistributionLists.Where(x => distributionListsToDelete.Contains(x.Id)).ExecuteDeleteAsync();

    }


    /// <summary>
    /// Cleans up campaigns without an inbox and their related Data.
    /// </summary>
    /// <returns></returns>
    public async Task CleanUpCampaignsWithoutInboxAsync() {

        bool hasMoreRecords = true;
        while (hasMoreRecords) {
            var cutOffDate = DateTimeOffset.UtcNow.AddDays(-_options.CampaignsWithoutInboxRetentionPeriodInDays);
            var query = DbContext.Campaigns.Where(x => !x.MessageChannelKind.HasFlag(MessageChannelKind.Inbox));
            query = query.Where(x => x.CreatedAt <= cutOffDate &&
            (!(x.ActivePeriod != null && x.ActivePeriod.From != null) || x.ActivePeriod.From <= cutOffDate)).OrderBy(x => x.CreatedAt).Take(_options.DeletionBatchSize);

            //unfortunately - I think that I have to materialize this
            var campaignDLIdss = await query.Select(x => new CampaignDLId { CampaignId = x.Id, DistributionListId = x.DistributionListId }).ToListAsync();
            if (campaignDLIdss.Count == 0) {
                break;
            }
            using (var transaction = await DbContext.Database.BeginTransactionAsync()) {
                try {
                    await DbContext.MessageEvents.Where(x => campaignDLIdss.Select(x => x.CampaignId).Contains(x.CampaignId)).ExecuteDeleteAsync();
                    await query.ExecuteDeleteAsync();
                    await DbContext.DistributionLists.Where(x => x.CreatedBy == "system" && campaignDLIdss.Select(x => x.DistributionListId)
                                                                                                          .Contains(x.Id)).ExecuteDeleteAsync();
                } 
                catch (Exception ex) {

                }
            }
        }
    }
}
