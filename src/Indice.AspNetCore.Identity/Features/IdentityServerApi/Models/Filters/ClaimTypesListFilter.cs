namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Contains filter when querying for claim types list.</summary>
public class ClaimTypesListFilter
{
    /// <summary>Determines whether this claim is required to create new users.</summary>
    public bool? Required { get; set; }
}
