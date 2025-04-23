using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Events;

/// <summary>The event that will be raised after failing to start the workflow for a case.</summary>
public class StartWorkflowFaultedEvent : ICaseEvent
{
    /// <summary>The case that has been submitted.</summary>
    public Case Case { get; }
    
    /// <summary>The case type code that has been submitted.</summary>
    public string CaseTypeCode { get; }
    
    /// <summary>The workflow Actor acting on the workflow.</summary>
    public UserActor WorkflowActor { get; }
    
    /// <summary>The Workflow Error.</summary>
    public string? Error { get; }

    /// <summary>Construct a new <see cref="StartWorkflowFaultedEvent"/>.</summary>
    /// <param name="case">The case that has been submitted.</param>
    /// <param name="caseTypeCode">The case type code that has been submitted.</param>
    /// <param name="workflowActor">The actor acting on the workflow.</param>
    /// <param name="error">The workflow Error.</param>
    public StartWorkflowFaultedEvent(Case @case, string caseTypeCode, UserActor workflowActor, string? error) {
        Case = @case;
        CaseTypeCode = caseTypeCode;
        WorkflowActor = workflowActor;
        Error = error;
    }
}