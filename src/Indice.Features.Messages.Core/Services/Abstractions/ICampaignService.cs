using Indice.Features.Messages.Core.Models;
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
    /// <param name="id">The id of the campaign.</param>
    Task<CampaignDetails> GetById(Guid id);
    /// <summary>Creates a new campaign.</summary>
    /// <param name="request">The data for the campaign to create.</param>
    Task<Campaign> Create(CreateCampaignRequest request);
    /// <summary>Updates an existing campaign.</summary>
    /// <param name="id">The id of the campaign.</param>
    /// <param name="request">The data for the campaign to update.</param>
    Task Update(Guid id, UpdateCampaignRequest request);
    /// <summary>Deletes an existing campaign.</summary>
    /// <param name="id">The id of the campaign.</param>
    Task Delete(Guid id);
    /// <summary>Publishes an existing campaign.</summary>
    /// <param name="id">The id of the campaign.</param>
    Task<Campaign> Publish(Guid id);
    /// <summary>Gets some statistics for the campaign.</summary>
    /// <param name="id">The id of the campaign.</param>
    Task<CampaignStatistics> GetStatistics(Guid id);
    /// <summary>Records a visit for the specified campaign.</summary>
    /// <param name="id">The id of the campaign.</param>
    Task UpdateHit(Guid id);
}
