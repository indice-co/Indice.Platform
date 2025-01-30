using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Models;
using Approval = Indice.Features.Cases.Workflows.Integration.Approval;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// A blocking activity that awaits signal from client.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
[Trigger(
    Category = "Cases - Approvals",
    DisplayName = "Await Approval",
    Description = "Handles the approval or rejection of a case.",
    Outcomes = new[] { nameof(Approval.Approve), nameof(Approval.Reject) }
)]
internal class AwaitApprovalActivity(CasesHttpClient casesClient) : BaseCaseActivity(casesClient)
{
    [ActivityInput(
        Label = "Role",
        Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = [SyntaxNames.Literal]
    )]
    public string? AllowedRole { get; set; } = string.Empty;

    [ActivityInput(
        Label = "Block previous approver",
        Hint = "Check this to block approvals from the same user."
    )]
    public bool BlockPreviousApprover { get; set; }

    [ActivityInput(
        Label = "Send approval comment to customer (if any)",
        Hint = "Show the approval comment of the selected actions to the customer or front-end of the application.",
        Options = new[] { nameof(Approval.Approve), nameof(Approval.Reject) },
        UIHint = ActivityInputUIHints.CheckList,
        DefaultSyntax = SyntaxNames.Json,
        SupportedSyntaxes = [SyntaxNames.Json, SyntaxNames.JavaScript]
    )]
    public IEnumerable<string> PublicActions { get; set; } = new List<string>();

    [ActivityOutput]
    public ApprovalRequest? Output { get; set; }

    [ActivityOutput]
    public string? Action { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        // Since we are writing a blocking activity, the activity needs to tell the workflow engine that execution should pause until an ApprovalRequest is received.
        // That will work, but only when the activity is used a blocking activity and not as a starting activity. If we used this as a starting activity,
        // what will happen is that when an ApprovalRequest is received, the workflow will begin, but gets suspended immediately after. That's no good.
        // Instead, what we want is for the workflow to continue to the next activity when an ApprovalRequest is received.
        // To make that work, we need to return a SuspendResult only if this is not the first pass.If it IS the first pass, we will simply return an OutcomeResult with the "Done" outcome.
        // https://v2.elsaworkflows.io/docs/guides/blocking-activities
        return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternalAsync(context) : Suspend();
    }

    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
        // That will achieve exactly what we need: when the activity is used as a starting activity, it will return "Done" and execution of the workflow will continue.
        // But when the activity is used as a blocking activity (i.e. not as the first activity of the workflow), the activity will suspend the workflow.           
        // The big idea is that we should be able to trigger workflows when an ApprovalRequest is received, regardless of whether we have workflows that use this as a starting trigger
        // or as a trigger to resume existing workflow instances.
        return await OnExecuteInternalAsync(context);
    }

    private Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
        var approval = context.Input as WorkflowSubmitApprovalRequest;
        
        //todo: create ApprovalOutput model, remove core dependency
        var action = Enum.Parse<Approval>(approval!.OutputAction.ToString());
        Output = new ApprovalRequest {
            Action = action,
            Comment = approval.OutputComment
        };
        Action = approval!.OutputAction.ToString();
        context.LogOutputProperty(this, "Output", Output);

        return Task.FromResult<IActivityExecutionResult>(Outcome(approval.OutputAction.ToString(), approval));
    }
}
