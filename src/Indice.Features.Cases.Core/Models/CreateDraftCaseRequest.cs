namespace Indice.Features.Cases.Core.Models;

/// <summary>The request payload for creating a new draft case.</summary>
public class CreateDraftCaseRequest
{
    /// <summary>The Case type code of the case.</summary>
    public string CaseTypeCode { get; set; } = null!;

    /// <summary>The group this case belongs to, eg a customer's branch</summary>
    public string? GroupId { get; set; }

    /// <summary>The Customer metadata.</summary>
    public ContactMeta? Owner { get; set; }

    /// <summary>A list of case metadata in key value pairs</summary>
    public Dictionary<string, string> Metadata { get; set; } = [];

    /// <summary>The channel that created the draft case. Defaults to <strong>Managed</strong> for the manage API and <strong>External</strong> for the my API.</summary>
    public string? Channel { get; set; }
    /// <summary>The reference number of the request that created the draft case</summary>
    public string? ReferenceNumber { get; set; }
    /// <summary>The key that correlates this case with other cases.</summary>
    /// <remarks>Also stored in <see cref="Metadata"/> under <c>ExternalCorrelationKey</c>, the way older clients send it. When both are sent, this one wins.</remarks>
    public string? RelationKey { get; set; }
}