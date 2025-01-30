using System.Text.Json;
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
    DisplayName = "Await Edit",
    Description = "Handles the edit of the data for case.",
    Outcomes = new[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Save }
)]
internal class AwaitEditActivity(CasesHttpClient casesClient) : BaseCaseActivity(casesClient)
{
    [ActivityInput(
        Label = "Role",
        Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = [SyntaxNames.Literal]
    )]
    public string? AllowedRole { get; set; }

    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternalAsync(context) : Suspend();
    }

    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
        return await OnExecuteInternalAsync(context);
    }

    // todo: when this throws we get a correct response
    private Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
        var editRequest = context.Input as WorkflowEditCaseRequest;
        Output = Newtonsoft.Json.Linq.JObject.Parse(JsonSerializer.Serialize(editRequest!.Data));
        context.LogOutputProperty(this, "Output", Output);
        return Task.FromResult<IActivityExecutionResult>(Outcome("Save", Output));
    }
}
