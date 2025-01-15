namespace Indice.Features.Cases.Workflows.Models;

/// <summary>Invoke Action Request Model.</summary>
public class InvokeActionRequest
{
    /// <summary>Id of the case.</summary>
    public Guid CaseId { get; set; }
    
    /// <summary>Id of the action.</summary>
    public Guid ActionId { get; set; }
    
    /// <summary>Value of the action.</summary>
    public string? Value { get; set; }
    
    /// <summary>The Actor.</summary>
    public Actor Actor { get; set; } = null!;
}