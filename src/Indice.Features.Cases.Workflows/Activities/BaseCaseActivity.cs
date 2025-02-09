using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Base Activity for Cases which provides hook for automatic case error handling.</summary>
public abstract class BaseCaseActivity(CasesHttpClient casesClient) : Activity
{
    /// <summary>The Cases Http client.</summary>
    protected readonly CasesHttpClient CasesClient = casesClient ?? throw new ArgumentNullException(nameof(casesClient));

    /// <summary>The Id of the case.</summary>
    [ActivityInput(
        Category = "Case Properties",
        Label = "CaseId",
        Hint = "The Id of the case. Leave it null to retrieve it from CorrelationId implicitly.",
        SupportedSyntaxes = [ SyntaxNames.JavaScript, SyntaxNames.Liquid ],
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public Guid? CaseId { get; set; } = null;

    /// <summary>Indicates if the base class will handle exceptions.</summary>
    [ActivityInput(
        Category = "Case Properties",
        Label = "HandleActivityError",
        Hint = "Activity errors will automatically move the case to the FaultedCheckpoint."
    )]
    public bool HandleActivityError { get; set; } = true;

    /// <summary>Override this to handle case error by yourself.</summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) {
        try {
            CaseId ??= Guid.Parse(context.CorrelationId);
            return await TryExecuteAsync(context);
        } catch (Exception exception) {
            if (HandleActivityError) {
                var message = $"Workflow Exception with DefinitionId \"{context.WorkflowInstance.DefinitionId}\" and InstanceId \"{context.WorkflowInstance.Id}\". Original exception message \"{exception.Message}\".";
                // await CaseMessageService.Send(CaseId!.Value, context.GetHttpContextUser()!, exception, message);
                await CasesClient.SendMessageAsync(CaseId!.Value, new WorkflowSendMessageRequest {
                    Message = new Message {
                        Comment = string.IsNullOrEmpty(message) 
                            ? $"Faulted with message: {exception.Message}" 
                            : $"Faulted with message: {message} and exception message: {exception.Message}",
                        PrivateComment = true
                    },
                    WorkflowActor = context.TryGetLastActor().ToCasesActor()
                });
            }
            
            throw;
        }
    }
    
    

    /// <summary>When this method is override and an exception occurs the case will move to a default faulted checkpoint.</summary>
    /// <returns></returns>
    public virtual ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        throw new NotImplementedException("Must be implemented in child class.");
    }

    /// <summary>Create a new comment to the case with the exception and the error.</summary>
    /// <param name="context"></param>
    /// <param name="exception">The exception.</param>
    /// <param name="message">The message to send as a comment.</param>
    protected async Task LogCaseError(ActivityExecutionContext context, Exception exception, string? message = null) {
        // Log to Elsa context
        context.LogOutputProperty(this, "Exception", exception);
        // Log to Case (via Comment)
        await CasesClient.SendMessageAsync(CaseId!.Value, new WorkflowSendMessageRequest {
            Message = new Message {
                Comment = string.IsNullOrEmpty(message)
                    ? $"Faulted with message: {exception.Message}"
                    : $"Faulted with message: {message} and exception message: {exception.Message}",
                PrivateComment = true
            },
            WorkflowActor = context.TryGetLastActor().ToCasesActor()
        });
    }
}
