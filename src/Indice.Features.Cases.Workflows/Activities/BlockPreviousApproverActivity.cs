using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integration;
using CaseApproval = Indice.Features.Cases.Workflows.Integration.CaseApproval;

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
    private readonly CasesHttpClient _casesClient;

    public BlockPreviousApproverActivity(CasesHttpClient casesClient)
        : base(casesClient) {
        _casesClient = casesClient;
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);

        CaseApproval lastApproval = null;
        // var lastApproval = await _caseApprovalService.GetLastApproval(CaseId.Value!); // todo: http or refactor
        if (lastApproval == null) {
            return Outcome(OutcomeNames.False);
        }

        var casesUser = context.TryGetLastActor();

        // var user = context.TryGetUser();
        if (casesUser.Id != lastApproval.CreatedBy.Id) {
            return Outcome(OutcomeNames.False);
        }
        
        await _casesClient.SendMessageAsync(CaseId.Value!, new Integration.Message {
            PrivateComment = true,
            Comment = "Already approved on the previous step. Self-assignment removed." // todo: we lose localization in elsa context
        });

        // await CaseMessageService.Send(CaseId.Value, user!, new Message {
        //     PrivateComment = true,
        //     Comment = _casesMessageDescriber.BlockPreviousApproverComment
        // });
        // await _casesClient.RemoveAssignmentAsync(CaseId.Value!);
        // await _adminCaseService.RemoveAssignment(CaseId.Value); // todo: http or refactor
        return Outcome(OutcomeNames.True);
    }
}
