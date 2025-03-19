namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Invoke Assignment Request Model.</summary>
public class InvokeAssignmentRequest
{
    /// <summary>Id of the case.</summary>
    public Guid CaseId { get; set; }
    
    /// <summary>The Actor.</summary>
    public Actor Actor { get; set; } = null!;
}