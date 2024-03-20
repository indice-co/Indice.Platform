using Indice.Types;

namespace Indice.Features.Cases.Models;

/// <summary>The Lookup Filter model.</summary>
public class LookupFilter
{
    /// <summary>A list of FilterTerms</summary>
    public List<FilterClause> FilterTerms { get; set; }
}
