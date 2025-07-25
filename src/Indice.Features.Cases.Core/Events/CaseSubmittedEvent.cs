using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core.Events;

/// <summary>The event that will be raised after a case's final submission.</summary>
public class CaseSubmittedEvent : ICaseEvent
{
    /// <summary>The case that has been submitted.</summary>
    public Case Case { get; }

    /// <summary>The case type code that has been submitted.</summary>
    public string CaseTypeCode { get; set; }
    
    /// <summary>The workflow Actor acting on the workflow.</summary>
    public UserActor WorkflowActor { get; set; }

    /// <summary>Construct a new <see cref="CaseSubmittedEvent"/>.</summary>
    /// <param name="case">The case that has been submitted.</param>
    /// <param name="caseTypeCode">The case type code that has been submitted.</param>
    /// <param name="workflowActor">The actor acting on the workflow.</param>
    public CaseSubmittedEvent(Case @case, string caseTypeCode, UserActor workflowActor) {
        Case = @case;
        CaseTypeCode = caseTypeCode;
        WorkflowActor = workflowActor;
    }
}