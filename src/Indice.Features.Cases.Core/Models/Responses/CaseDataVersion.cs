namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>A version of the data of a case. Every time the case data change a new version is stored.</summary>
public class CaseDataVersion
{
    /// <summary>The id of the version.</summary>
    public Guid Id { get; set; }

    /// <summary>The user that created the version and when.</summary>
    public AuditMeta CreatedBy { get; set; } = new AuditMeta();

    /// <summary>The json data of the version.</summary>
    public dynamic? Data { get; set; }

    /// <summary>Whether this is the current (latest private) data of the case.</summary>
    public bool IsCurrent { get; set; }

    /// <summary>Whether this is the data published to the customer.</summary>
    public bool IsPublic { get; set; }
}
