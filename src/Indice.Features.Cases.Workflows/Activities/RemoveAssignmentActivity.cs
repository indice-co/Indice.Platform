using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Clear the assignedTo property for a Case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Remove Assignment from user",
    Description = "Remove the assignment of a case.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class RemoveAssignmentActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        await CasesManager.RemoveAssignment(CaseId.Value);
        return Done();
    }
}