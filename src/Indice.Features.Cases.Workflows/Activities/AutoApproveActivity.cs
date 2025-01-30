using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Auto approve case",
    Description = "Add an approval as the previous user that edited/created the case. Use this approach when AwaitApproval Activity is not running.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class AutoApproveActivity : BaseCaseActivity
{
    private readonly CasesHttpClient _casesClient;

    public AutoApproveActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient ?? throw new ArgumentNullException(nameof(casesClient));
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // await _casesClient.AddApproval(); // todo: add api call
        // await _caseApprovalService.AddApproval(CaseId.Value, null, context.TryGetUser()!, Approval.Approve, reason:null);
        return Done();
    }
}