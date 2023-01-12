using System.Security.Claims;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Services;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Activities
{
    /// <summary>
    /// A blocking activity that awaits signal from client.
    /// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
    /// </summary>
    [Trigger(
        Category = "Cases",
        DisplayName = "Await Edit",
        Description = "Handles the edit of the data for case.",
        Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Save }
    )]
    internal class AwaitEditActivity : BaseCaseActivity
    {
        private readonly IAdminCaseMessageService _caseMessageService;
        private readonly CasesMessageDescriber _casesMessageDescriber;

        public AwaitEditActivity(
            IAdminCaseMessageService caseMessageService,
            CasesMessageDescriber casesMessageDescriber)
            : base(caseMessageService) {
            _caseMessageService = caseMessageService;
            _casesMessageDescriber = casesMessageDescriber;
        }

        [ActivityInput(
            Label = "Role",
            Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string AllowedRole { get; set; }

        [ActivityOutput]
        public string Output { get; set; }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternalAsync(context) : Suspend();
        }

        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
            return await OnExecuteInternalAsync(context);
        }

        private async Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);
            var caseData = context.Input as string;
            var user = context.TryGetUser();
            await _caseMessageService.Send(CaseId!.Value,
                context.GetHttpContextUser()!,
                new Message {
                    Data = caseData,
                    Comment = _casesMessageDescriber.EditCaseComment(user.FindDisplayName(), user.FindFirstValue(BasicClaimTypes.Email)),
                    PrivateComment = true
                });
            Output = caseData!;
            context.LogOutputProperty(this, "Output", caseData);
            return Outcome("Save", caseData);
        }
    }
}
