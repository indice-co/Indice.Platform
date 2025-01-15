namespace Indice.Features.Cases.Workflows.Models;

public class WorkflowCasesActions
{
    /// <summary>User can assign the case to himself.</summary>
    public bool HasAssignment { get; set; }

    /// <summary>User can remove the assignment of the case.</summary>
    public bool HasUnassignment { get; set; }

    /// <summary>User can edit the case data.</summary>
    public bool HasEdit { get; set; }

    /// <summary>User can approve/reject the case.</summary>
    public bool HasApproval { get; set; }

    /// <summary>The list of custom action blocking activities that will generate the corresponding components.</summary>
    public List<WorkflowCustomCaseAction> CustomActions { get; set; } = [];
}