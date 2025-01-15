namespace Indice.Features.Cases.Core.Models;

/// <summary>Represents the Workflow Actor acting on the workflow.</summary>
public class WorkflowActor
{
    /// <summary>The Id of the user.</summary>
    public string? Id { get; set; }
    
    /// <summary>Can be the customer id or something related to an external system correlation id</summary>
    public string? Reference { get; set; }

    /// <summary>The name of the user.</summary>
    public string? Name { get; set; }

    /// <summary>The email of the user.</summary>
    public string? Email { get; set; }

    internal AuditMeta ToAuditMeta() =>
        new() {
            Id = Id,
            Name = Name,
            Email = Email
        };
}