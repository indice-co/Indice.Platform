using System.Threading.Tasks;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces
{
    /// <summary>
    /// Lookup service.
    /// </summary>
    public interface ILookupService
    {
        /// <summary>
        /// Get the <see cref="ResultSet{T}"/> of a <see cref="LookupItem"/> for a specific lookup name.
        /// </summary>
        /// <param name="lookupName">The lookup name.</param>
        /// <param name="searchValues">Any search values to filter the results.</param>
        /// <returns></returns>
        Task<ResultSet<LookupItem>> Get(string lookupName, string searchValues = null);
    }
}
