using System;
using System.Reflection;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Extensions;

namespace Indice.Features.Cases.Workflows.Activities
{
    /// <summary>
    /// A blocking activity that awaits signal from client.
    /// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
    /// </summary>
    [Trigger(
        Category = "Cases",
        DisplayName = "Await Custom Outcome",
        Description = "Handles the custom outcome and forwards case data.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class AwaitActionActivity : BaseCaseActivity
    {
        private readonly IAdminCaseMessageService _caseMessageService;

        /// <inheritdoc />
        public AwaitActionActivity(IAdminCaseMessageService caseMessageService) : base(caseMessageService) {
            _caseMessageService = caseMessageService;
        }

        /// <summary>
        /// The Id of the action that will trigger the activity. It's hidden from the elsa dashboard and gets a unique value automatically.
        /// </summary>
        [ActivityInput(IsBrowsable = false)]
        public string? ActionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The name of the action button to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Name Name",
            Hint = "The name of the action button to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string? ActionName { get; set; }

        /// <summary>
        /// The description of the action button to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Name Description",
            Hint = "The description of the action button to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string? ActionDescription { get; set; }

        /// <summary>
        /// Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.
        /// </summary>
        [ActivityInput(
            Label = "Role",
            Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string? AllowedRole { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the Back-Office UI wil have the input element.
        /// </summary>
        [ActivityInput(
            Label = "Show Input to Back-Office UI",
            Hint = "Show an Input field to Back-Office UI and send the value to the output of this activity."
        )]
        public bool ShowInput { get; set; } = false;

        /// <summary>
        /// The case data.
        /// </summary>
        [ActivityOutput]
        public object Output { get; set; }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternal(context) : Suspend();
        }

        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
            return await OnExecuteInternal(context);
        }

        private async Task<IActivityExecutionResult> OnExecuteInternal(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);

            var input = context.Input as CustomActionRequest;

            Output = input?.Value ?? string.Empty;
            context.LogOutputProperty(this, nameof(Output), Output);

            await _caseMessageService.Send(CaseId.Value, context.TryGetUser()!, new Message {
                Comment = $"Action \"{ActionName}\" executed successfully with value \"{Output}\".",
                PrivateComment = true
            });

            return Done(Output);
        }
    }
}