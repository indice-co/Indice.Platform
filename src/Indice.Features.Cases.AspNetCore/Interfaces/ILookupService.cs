using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Lookup service Interface.
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Get the <see cref="ResultSet{T}"/> of a <see cref="LookupItem"/> for a specific lookup name.
        /// </summary>
        /// <param name="searchValues">Any search values to filter the results.</param>
        /// <returns></returns>
        Task<ResultSet<LookupItem>> Get(SearchValues searchValues = null);
    }
}
