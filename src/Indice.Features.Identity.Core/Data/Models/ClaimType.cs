﻿namespace Indice.Features.Identity.Core.Data.Models;

/// <summary>Models an application claim type.</summary>
public class ClaimType
{
    /// <summary>Determines whether this claim is required to create new users.</summary>
    public bool Required { get; set; }
    /// <summary>Determines whether this is a system reserved claim.</summary>
    public bool Reserved { get; set; }
    /// <summary>Determines whether this claim will be editable by a user if exposed through a public API.</summary>
    public bool UserEditable { get; set; }
    /// <summary>A description.</summary>
    public string? Description { get; set; }
    /// <summary>The unique id of the claim.</summary>
    public string Id { get; set; } = null!;
    /// <summary>The name.</summary>
    public string Name { get; set; } = null!;
    /// <summary>The name used for display purposes.</summary>
    public string? DisplayName { get; set; }
    /// <summary>A regular expression rule that constraints the values of the claim.</summary>
    public string? Rule { get; set; }
    /// <summary>The value type of the claim. </summary>
    public ClaimValueType ValueType { get; set; }
}

