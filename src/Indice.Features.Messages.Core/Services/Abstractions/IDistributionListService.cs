using Indice.Features.Messages.Core.Models;
using Indice.Features.Messages.Core.Models.Requests;
using Indice.Types;

namespace Indice.Features.Messages.Core.Services.Abstractions
{
    /// <summary>
    /// A service that contains distribution list related operations.
    /// </summary>
    public interface IDistributionListService
    {
        /// <summary>
        /// Creates a new distribution list.
        /// </summary>
        /// <param name="request">The data for the distribution list to create.</param>
        Task<DistributionList> Create(CreateDistributionListRequest request);
        /// <summary>
        /// Gets a distribution list by it's unique id.
        /// </summary>
        /// <param name="id">The id of the distribution list.</param>
        Task<DistributionList> GetById(Guid id);
        /// <summary>
        /// Gets a distribution list by it's name.
        /// </summary>
        /// <param name="name">The name of the distribution list.</param>
        Task<DistributionList> GetByName(string name);
        /// <summary>
        /// Gets a list of all distribution lists in the system.
        /// </summary>
        /// <param name="options">List parameters used to navigate through collections. Contains parameters such as sort, search, page number and page size.</param>
        Task<ResultSet<DistributionList>> GetList(ListOptions options);
    }
}
