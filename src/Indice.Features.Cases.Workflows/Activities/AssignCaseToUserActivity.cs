using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Assign case to user",
    Description = "Assign the case to a back-office user.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
)]
internal class AssignCaseToUserActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    [ActivityInput(
        Label = "User",
        Hint = "The AuditMeta object of the user to assign the case",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = [SyntaxNames.JavaScript]
    )]
    public WorkflowActor User { get; set; } = new();

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        try {
            await CasesManager.AssignAsync(CaseId.Value, User);
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome(CustomOutcomeNames.Failed);
        }
        
        return Done();
    }
}