namespace Indice.Features.Cases.Workflows.Models;

public class WorkflowAssignCaseRequest
{
    public Guid CaseId { get; set; }
    
    public string OutcomeResult { get; set; } // todo: move to enum and share model?
    
    public CasesUser CasesUser { get; set; }
}