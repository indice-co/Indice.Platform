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

    /// <summary>The email of the user.</summary>
    public string? Email { get; init; }
    
    /// <summary>The current culture of the user.</summary>
    public string? CurrentCulture { get; init; }

    internal AuditMeta ToAuditMeta() =>
        new() {
            Id = Id,
            Name = Name,
            Email = Email
        };
}