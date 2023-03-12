using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Services;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Block the previous approver if it is the same user and prevent from continuing the workflow.</summary>
[Activity(
    Category = "Cases - Approvals",
    DisplayName = "Block previous approver",
    Description = "Block the previous approver if it is the same user and prevent from continuing the workflow.",
    Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
)]
internal class BlockPreviousApproverActivity : BaseCaseActivity
{
    private readonly IAdminCaseMessageService _caseMessageService;
    private readonly ICaseApprovalService _caseApprovalService;
    private readonly IAdminCaseService _adminCaseService;
    private readonly CasesMessageDescriber _casesMessageDescriber;

    public BlockPreviousApproverActivity(
        IAdminCaseMessageService caseMessageService,
        ICaseApprovalService caseApprovalService, 
        IAdminCaseService adminCaseService, 
        CasesMessageDescriber casesMessageDescriber)
        : base(caseMessageService) {
        _caseMessageService = caseMessageService;
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        _casesMessageDescriber = casesMessageDescriber ?? throw new ArgumentNullException(nameof(casesMessageDescriber));
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);

        var lastApproval = await _caseApprovalService.GetLastApproval(CaseId.Value!);
        if (lastApproval == null) {
            return Outcome(OutcomeNames.False);
        }

        var user = context.TryGetUser();
        if (user.FindSubjectId() != lastApproval.CreatedBy.Id) {
            return Outcome(OutcomeNames.False);
        }

        await _caseMessageService.Send(CaseId.Value, user!, new Message {
            PrivateComment = true,
            Comment = string.Format(CasesResources.Culture, _casesMessageDescriber.BlockPreviousApproverComment)
        });
        await _adminCaseService.RemoveAssignment(CaseId.Value);
        return Outcome(OutcomeNames.True);
    }
}
