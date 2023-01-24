using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Providers.WorkflowStorage;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Extensions;

namespace Indice.Features.Cases.Workflows.Activities
{
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
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid },
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName
        )]
        public Message Message { get; set; }

        [ActivityOutput]
        public object Output { get; set; }

        public override async ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context) {
            var user = context.TryGetUser();
            if (user == null || !user.Identity.IsAuthenticated) {
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
}
