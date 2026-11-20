using Indice.Features.Identity.Core.Data.Models;

namespace Indice.Features.Identity.UI.Models;

/// <summary>A claim that is marked as user editable (<see cref="ClaimType.UserEditable"/>) and is shown on the profile pages.</summary>
public class ProfileClaimViewModel
{
    /// <summary>The claim type.</summary>
    public string Type { get; set; } = null!;
    /// <summary>The name used for display purposes.</summary>
    public string DisplayName { get; set; } = null!;
    /// <summary>A description of the claim.</summary>
    public string? Description { get; set; }
    /// <summary>The value type of the claim.</summary>
    public ClaimValueType ValueType { get; set; }
    /// <summary>Indicates whether a value is required.</summary>
    public bool Required { get; set; }
    /// <summary>A regular expression that the whole value must match.</summary>
    public string? Rule { get; set; }
    /// <summary>The current value of the claim, formatted for an HTML input (dates as <c>yyyy-MM-dd</c>, booleans as <c>true</c>/<c>false</c>).</summary>
    public string? Value { get; set; }
}
