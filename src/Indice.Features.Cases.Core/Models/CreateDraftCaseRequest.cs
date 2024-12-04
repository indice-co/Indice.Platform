﻿namespace Indice.Features.Cases.Core.Models;

/// <summary>The request payload for creating a new draft case.</summary>
public class CreateDraftCaseRequest
{
    /// <summary>The Case type code of the case.</summary>
    public string CaseTypeCode { get; set; } = null!;

    /// <summary>The group this case belongs to, eg a customer's branch</summary>
    public string? GroupId { get; set; }

    /// <summary>The Customer metadata.</summary>
    public CustomerMeta? Customer { get; set; }

    /// <summary>A list of case metadata in key value pairs</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>The channel that created the draft case</summary>
    public string? Channel { get; set; }
}