using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
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
    DisplayName = "Await Edit",
    Description = "Handles the edit of the data for case.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Save }
)]
public class AwaitEditActivity(ICasesManager casesManager) : BaseBlockingActivity(casesManager)
{
    /// <summary>Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.</summary>
    [ActivityInput(
        Label = "Role",
        Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = [SyntaxNames.Literal]
    )]
    public string? AllowedRole { get; set; }

    /// <summary>Case Data after Editing.</summary>
    [ActivityOutput]
    public object? Output { get; set; }

    /// <inheritdoc />
    protected override async Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var editRequest = context.Input as InvokeEditRequest;
        var caseData = editRequest!.Data;

        await CasesManager.Send(CaseId.Value, context.TryGetLastActor(), new Message {
            Data = caseData,
            Comment = editRequest.Comment,
            PrivateComment = true
        });
        
        Output = caseData;
        context.LogOutputProperty(this, "Output", caseData);
        context.SetVariable(CasesWorkflowConstants.WorkflowVariables.Actor.Current, editRequest.Actor);
        return Outcome("Save", caseData);
    }
}
