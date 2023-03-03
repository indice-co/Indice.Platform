using System.ComponentModel.DataAnnotations;
using Indice.AspNetCore.Identity.Api.Attributes;
using Indice.Configuration;
using Indice.Features.Identity.Core.Data.Models;

namespace Indice.AspNetCore.Identity.Api.Models;

/// <summary>Models a claim type that will be updated on the server.</summary>
public class UpdateClaimTypeRequest
{
    /// <summary>The name used for display purposes.</summary>
    [MaxLength(TextSizePresets.M128)]
    public string DisplayName { get; set; }
    /// <summary>A description.</summary>
    [MaxLength(TextSizePresets.L1024)]
    public string Description { get; set; }
    /// <summary>Determines whether this claim is required to create new users.</summary>
    public bool Required { get; set; }
    /// <summary>Determines whether this claim will be editable by a user if exposed through a public API.</summary>
    public bool UserEditable { get; set; }
    /// <summary>A regex rule that constraints the values of the claim.</summary>
    [ValidRegularExpression]
    [MaxLength(TextSizePresets.M512)]
    public string Rule { get; set; }
    /// <summary>The value type of the claim.</summary>
    public ClaimValueType ValueType { get; set; }
}
