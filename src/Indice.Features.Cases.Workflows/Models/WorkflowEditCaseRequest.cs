using System.Text.Json;

namespace Indice.Features.Cases.Workflows.Models;

public class WorkflowEditCaseRequest
{
    public Guid CaseId { get; set; }
    public JsonElement Data { get; set; }
    
    public CasesUser CasesUser { get; set; }
}