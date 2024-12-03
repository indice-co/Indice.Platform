using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Workflows.Extensions;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Auto approve case",
    Description = "Add an approval as the previous user that edited/created the case. Use this approach when AwaitApproval Activity is not running.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class AutoApproveActivity : BaseCaseActivity
{
    private readonly ICaseApprovalService _caseApprovalService;

    public AutoApproveActivity(
        IAdminCaseMessageService caseMessageService,
        ICaseApprovalService caseApprovalService) : base(caseMessageService) {
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        await _caseApprovalService.AddApproval(CaseId.Value, null, context.TryGetUser()!, Approval.Approve, reason:null);
        return Done();
    }
}