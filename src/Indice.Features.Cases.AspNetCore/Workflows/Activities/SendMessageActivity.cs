using System.Security.Claims;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Features.Cases.Workflows.Extensions;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Send Message",
    Description = "Send a message to a case in order to change the active checkpoint, update its data, add an attachment or add a comment. The current context user will be responsible for the change.",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class SendMessageActivity : BaseCaseActivity
{
    private readonly IAdminCaseMessageService _caseMessageService;

    public SendMessageActivity(
        IAdminCaseMessageService caseMessageService)
        : base(caseMessageService) {
        _caseMessageService = caseMessageService;
    }

    [ActivityInput(
        Label = "Message",
        Hint = "The message to send to the case service.",
        SupportedSyntaxes = [SyntaxNames.JavaScript, SyntaxNames.Liquid],
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public Message Message { get; set; } = null!;


    [ActivityInput(
        Label = nameof(RunAsSystemUser),
        Hint =
            "Select this option if you want to override the user context and log the message as system user. This is useful to override Membership Authorization for checkpoint movements.",
        UIHint = ActivityInputUIHints.Checkbox,
        DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
    )]
    public bool RunAsSystemUser { get; set; } = false;

    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) {
        var user = RunAsSystemUser 
            ? CasesClaimsPrincipalExtensions.SystemUser() 
            : context.TryGetUser();

        if (user is null || !user.Identity!.IsAuthenticated) {
            throw new Exception("User not found or not authenticated");
        }

        CaseId ??= Guid.Parse(context.CorrelationId); // Because we are not triggering base.TryExecuteAsync we need to declare it again.

        try {
            await _caseMessageService.Send(CaseId.Value, user, Message);
        } catch (Exception exception) {
            Output = exception.Message;
            context.LogOutputProperty(this, "Output", exception);
            return Outcome("Failed");
        }

        Output = Message.CheckpointTypeName;
        context.LogOutputProperty(this, "Output", Message.CheckpointTypeName);
        return Outcome(OutcomeNames.Done, Message.CheckpointTypeName);
    }
}
