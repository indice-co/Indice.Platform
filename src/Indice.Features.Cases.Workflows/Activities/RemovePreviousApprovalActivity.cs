using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Remove previous approval",
    Description = "Remove the previous approval action.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class RemovePreviousApprovalActivity : BaseCaseActivity
{
    private readonly CasesHttpClient _casesClient;

    public RemovePreviousApprovalActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient;
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // await _casesClient.RollBackApproval(CaseId.Value);
        // await _caseApprovalService.RollbackApproval(CaseId.Value);
        return Done();
    }
}