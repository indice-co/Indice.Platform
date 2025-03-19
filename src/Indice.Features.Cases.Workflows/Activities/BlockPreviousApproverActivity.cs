using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integrations;
using CaseApproval = Indice.Features.Cases.Workflows.Integrations.CaseApproval;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Block the previous approver if it is the same user and prevent from continuing the workflow.</summary>
[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Block previous approver",
    Description = "Block the previous approver if it is the same user and prevent from continuing the workflow.",
    Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
)]
internal class BlockPreviousApproverActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);

        CaseApproval lastApproval;
        try {
            lastApproval = await CasesManager.GetLastApprovalAsync(CaseId.Value);
        } catch (ApiException) {
            return Outcome(OutcomeNames.False);
        }
        
        if (context.TryGetLastActor().Id != lastApproval.CreatedBy.Id) {
            return Outcome(OutcomeNames.False);
        }

        await CasesManager.BlockPreviousApproverAsync(CaseId.Value, context.TryGetLastActor().ToCasesActor());
        
        return Outcome(OutcomeNames.True);
    }
}
