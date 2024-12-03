﻿namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>The reject reason dto.</summary>
public class RejectReason
{
    /// <summary>The key of the reject reason. This key will be used in resources.</summary>
    public string? Key { get; set; }

    /// <summary>The value of the reject reason. This will be translated into request language.</summary>
    public string? Value { get; set; }
}
