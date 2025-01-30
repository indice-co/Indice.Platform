using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Models;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// A blocking activity that awaits signal from client.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
[Trigger(
    Category = "Cases",
    DisplayName = "Await Assignment",
    Description = "When a user triggers this activity, they will assign the current workflow case to themselves.",
    Outcomes = new[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class AwaitAssignmentActivity : BaseCaseActivity
{
    private readonly CasesHttpClient _casesClient;
    
    public AwaitAssignmentActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient ?? throw new ArgumentNullException(nameof(casesClient));
    }

    [ActivityInput(
        Label = "Role",
        Hint = "User role that can assign a case to self. If left blank, any authenticated user can assign a case to them.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = [SyntaxNames.Literal]
    )]
    public string? AllowedRole { get; set; }

    [ActivityOutput]
    public CasesUser? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternal(context) : Suspend();
    }

    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
        return await OnExecuteInternal(context);
    }

    private async Task<IActivityExecutionResult> OnExecuteInternal(ActivityExecutionContext context) {
        var assignment = context.Input as WorkflowAssignCaseRequest;

        if (assignment?.OutcomeResult != OutcomeNames.Done) {
            return Outcome(CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed);
        }
        
        Output = assignment.CasesUser;
        context.LogOutputProperty(this, "Output", Output);
        return Done();
    }
}