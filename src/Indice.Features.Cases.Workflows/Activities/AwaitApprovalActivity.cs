using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Models;
using Approval = Indice.Features.Cases.Workflows.Integrations.Approval;

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
public class AwaitApprovalActivity(ICasesManager casesManager) : BaseBlockingActivity(casesManager)
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
    public ApprovalOutput? Output { get; set; }

    [ActivityOutput]
    public string? Action { get; set; }

    /// <inheritdoc />
    protected override async Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var approval = context.Input as InvokeApprovalRequest;

        // Set activity's output properties 
        Output = new ApprovalOutput {
            Action = approval!.Action,
            Comment = approval.Comment
        };
        Action = approval.Action.ToString();
        context.LogOutputProperty(this, "Output", Output);
        
        await CasesManager.AddApprovalWithCommentAsync(CaseId.Value, new WorkflowAddApprovalWithCommentRequest {
            Action = Enum.Parse<Approval>(approval.Action.ToString()),
            Reason = approval.Comment,
            PrivateComment = !PublicActions.Contains(approval.Action.ToString()),
            WorkflowActor = approval.Actor.ToCasesActor()
        });
        
        context.SetVariable(CasesWorkflowConstants.WorkflowVariables.Actor.Current, approval.Actor);
        
        return Outcome(approval.Action.ToString(), approval);
    }
}

/// <summary>The Output of the Activity.</summary>
public class ApprovalOutput
{
    /// <summary>Action for approval.</summary>
    public WorkflowApproval Action { get; set; }
    
    /// <summary>Comment related to the action.</summary>
    public string? Comment { get; set; }
}