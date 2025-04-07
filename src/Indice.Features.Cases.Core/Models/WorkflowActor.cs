namespace Indice.Features.Cases.Core.Models;

/// <summary>Represents the Workflow Actor acting on the workflow.</summary>
public class WorkflowActor
{
    /// <summary>The Id of the user.</summary>
    public required string Id { get; init; }

    /// <summary>Can be the customer id or something related to an external system correlation id.</summary>
    public string? Reference { get; init; }

    /// <summary>The group id claim value.</summary> 
    public string? GroupId { get; init; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; init; }
    /// <summary>The name of the user.</summary>
    public string? Tin { get; init; }
    /// <summary>The email of the user.</summary>
    public string? Email { get; init; }
    /// <summary>The current culture of the user.</summary>
    public string? CurrentCulture { get; init; }
    /// <summary>Indicates if the currect actor is either a SystemClient or an administrator.</summary>
    public bool IsSystemClient { get; init; }
    /// <summary>Indicates if the currect actor is either a cases administrator or a admin.</summary>
    public bool IsAdmin { get; init; }
    public List<string> Roles { get; init; } = new();
    internal AuditMeta ToAuditMeta() =>
        new() {
            Id = Id,
            Name = Name,
            Email = Email
        };
}