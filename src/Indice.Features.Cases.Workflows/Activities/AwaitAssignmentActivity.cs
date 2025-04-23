using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Features.Cases.Workflows.Models;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// A blocking activity that awaits signal from client.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
[Trigger(
    Category = "Cases",
    DisplayName = "Await Assignment",
    Description = "When a user triggers this activity, they will assign the current workflow case to themselves.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
)]
 public class AwaitAssignmentActivity(ICasesManager casesManager) : BaseBlockingActivity(casesManager)
{
    /// <summary>User role that can assign a case to self.</summary>
    [ActivityInput(
        Label = "Role",
        Hint = "User role that can assign a case to self. If left blank, any authenticated user can assign a case to them.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = [SyntaxNames.Literal]
    )]
    public string? AllowedRole { get; set; }

    /// <summary>Assignee Audit Meta.</summary>
    [ActivityOutput]
    public AuditMeta? Output { get; set; }
    
    /// <inheritdoc />
    protected override async Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var assignment = context.Input as InvokeAssignmentRequest;

        AuditMeta assignedTo;
        try {
            assignedTo = await CasesManager.AssignToActor(assignment!.Actor.ToCasesActor(), CaseId!.Value);
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome(CustomOutcomeNames.Failed);
        }

        Output = assignedTo;
        context.LogOutputProperty(this, "Output", Output);
        context.SetVariable(CasesWorkflowConstants.WorkflowVariables.Actor.Current, assignment.Actor);
        return Done();
    }
}