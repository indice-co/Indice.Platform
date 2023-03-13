using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;

namespace Indice.Features.Cases.Interfaces;

/// <summary>Lookup service Interface.</summary>
public interface ILookupService
{
    /// <summary>Get the <see cref="ResultSet{T}"/> of a <see cref="LookupItem"/> for a specific lookup name.</summary>
    /// <param name="options">Any options to filter the lookup results.</param>
    /// <returns></returns>
    Task<ResultSet<LookupItem>> Get(ListOptions<LookupFilter> options = null);
}
