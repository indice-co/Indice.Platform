namespace Indice.Features.Identity.UI.Models;

/// <summary>The consented (grants) view model.</summary>
public class GrantsViewModel
{
    /// <summary>The given grants list</summary>
    public IEnumerable<GrantModel>? Grants { get; set; }
}
