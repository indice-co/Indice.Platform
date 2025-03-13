using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Auto approve case",
    Description = "Add an approval as the previous user that edited/created the case. Use this approach when AwaitApproval Activity is not running.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class AutoApproveActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        await CasesManager.AddApprovalAsync(CaseId.Value, new WorkflowAddApprovalRequest {
            Action = Approval.Approve,
            Reason = null,
            WorkflowActor = context.TryGetLastActor().ToCasesActor()
        });
        return Done();
    }
}