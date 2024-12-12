namespace Indice.Features.Cases.Core.Models;

/// <summary>The Lookup Filter model.</summary>
public class LookupFilter
{
    /// <summary>A list of FilterTerms</summary>
    public FilterTerm[] FilterTerms { get; set; } = [];
}
