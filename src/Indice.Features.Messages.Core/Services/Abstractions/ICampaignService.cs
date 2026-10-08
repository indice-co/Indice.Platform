using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Kpis;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions;

/// <summary>A service that contains campaign related operations.</summary>
public interface ICampaignService
{
    /// <summary>Gets a list of all campaigns in the system.</summary>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    Task<ResultSet<Campaign>> GetList(ListOptions<CampaignListFilter> options);
    /// <summary>Gets a campaign by it's unique id.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    Task<CampaignDetails?> GetById(Guid campaignId);
    /// <summary>Creates a new campaign.</summary>
    /// <param name="request">The data for the campaign to create.</param>
    Task<Campaign> Create(CreateCampaignRequest request);
    /// <summary>Updates an existing campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <param name="request">The data for the campaign to update.</param>
    Task Update(Guid campaignId, UpdateCampaignRequest request);
    /// <summary>Deletes an existing campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    Task Delete(Guid campaignId);
    /// <summary>Publishes an existing campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    Task<Campaign> Publish(Guid campaignId);
    /// <summary>Gets some statistics for the campaign.</summary>
    /// <param name="asOfDate">The date and time to use as a reference point for calculating the metrics. If not provided, the current date and time will be used.</param>
    Task<CampaignMetrics> GetMetrics(DateTimeOffset? asOfDate = null);
    /// <summary>Gets some statistics for the campaign performance or all stats.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <remarks>If campaignId is not specified then the metrics are calculated via thier average values in total</remarks>
    Task<RecipientMetrics?> GetRecipientMetrics(Guid? campaignId = null);
    /// <summary>Records a visit for the specified campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    Task UpdateHit(Guid campaignId);
    /// <summary>Gets the sent volume of messages per channel kind.</summary>
    /// <param name="campaignId">The id of the campaign. Optional</param>
    Task<Dictionary<string, int>> GetChannelMetrics(Guid? campaignId = null);
    /// <summary>Gets the  volume of messages per message type.</summary>
    /// <param name="onDate">The day for which to calculate the volume. If null then the volume will be calculated for the entire dataset</param>
    /// <param name="limit">Limit top results. Defaults to <c>5</c></param>
    /// <remarks>The method will order results by volume descenting and limit the top <paramref name="limit"/> number.</remarks>
    Task<List<Volume<MessageType>>> GetMessageTypeMetrics(DateTimeOffset? onDate = null, int limit = 5);
    /// <summary>Gets a list of all messages populated for this campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
    Task<ResultSet<Recipient>> GetCampaignRecipients(Guid campaignId, ListOptions options);
    /// <summary>Gets the details of a specific message for a campaign.</summary>
    /// <param name="campaignId">The id of the campaign.</param>
    /// <param name="contactId">The id of the contact.</param>
    Task<RecipientMessageEvents> GetCampaignRecipientDetails(Guid campaignId, Guid contactId);
}
