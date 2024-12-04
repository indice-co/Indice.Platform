﻿namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The lookup item model.</summary>
public class LookupItem
{
    /// <summary>The name or the key of the look up item</summary>
    public string Name { get; set; } = null!;

    /// <summary>The value of the lookup item</summary>
    public string Value { get; set; } = null!;
}
