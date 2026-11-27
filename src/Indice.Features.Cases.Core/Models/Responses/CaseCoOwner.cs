namespace Indice.Features.Cases.Core.Models.Responses;

/// <summary>A co-owner of a case, as seen by the back-office.</summary>
public class CaseCoOwner
{
    /// <summary>The user id of the co-owner.</summary>
    public string? UserId { get; set; }
    /// <summary>The customer id, or the correlation id of an external system.</summary>
    public string? Reference { get; set; }
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The Tax identification number.</summary>
    public string? Tin { get; set; }
    /// <summary>The permission of the co-owner on the case.</summary>
    public ContactPermission Permission { get; set; }
    /// <summary>The user that added the co-owner, and when.</summary>
    public AuditMeta AddedBy { get; set; } = null!;
}

/// <summary>A co-owner of a case, as seen by the customers that take part in the case.</summary>
public class MyCaseCoOwner
{
    /// <summary>The user id of the co-owner.</summary>
    public string? UserId { get; set; }
    /// <summary>The first name.</summary>
    public string? FirstName { get; set; }
    /// <summary>The last name.</summary>
    public string? LastName { get; set; }
    /// <summary>The permission of the co-owner on the case.</summary>
    public ContactPermission Permission { get; set; }
}
