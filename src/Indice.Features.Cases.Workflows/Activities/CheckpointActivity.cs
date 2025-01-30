using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Checkpoint",
    Description = "Change the active checkpoint. The current context last actor will be responsible for the change.",
    Outcomes = new[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed }
)]
public class CheckpointActivity : BaseCaseActivity
{
    private CasesHttpClient _casesClient;

    public CheckpointActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient;
    }
    
    // todo: make required
    [ActivityInput(
        Label = "Checkpoint Name",
        Hint = "The name of the checkpoint to move the workflow to.",
        SupportedSyntaxes = [SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid],
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public string? CheckpointTypeName { get; set; }
    
    [ActivityInput(
        Label = "Comment",
        Hint = "The comment to add to the checkpoint.",
        SupportedSyntaxes = [SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid],
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public string? Comment { get; set; }
    
    [ActivityInput(
        Label = "Private Comment.",
        Hint = "Indicates if the comment should be visible to the customer."
    )]
    public bool PrivateComment { get; set; }
    
    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId); // Because we are not triggering base.TryExecuteAsync we need to declare it again.
        try {
            await _casesClient.MoveToCheckpointAsync(CaseId.Value, CheckpointTypeName, string.IsNullOrWhiteSpace(Comment) ? null : Comment, PrivateComment);
        } catch (Exception ex) {
            Output = ex.Message;
            context.LogOutputProperty(this, "Output", ex);
            return Outcome("Failed");
        }
        
        Output = CheckpointTypeName;
        context.LogOutputProperty(this, "Output", CheckpointTypeName);
        return Outcome(OutcomeNames.Done, CheckpointTypeName);
    }
}