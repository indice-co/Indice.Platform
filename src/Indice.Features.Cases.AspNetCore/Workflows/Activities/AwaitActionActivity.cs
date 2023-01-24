using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
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
        DisplayName = "Await Action",
        Description = "A blocking activity that handles a custom action.",
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
        public string ActionId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The name of the action button to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Action Name",
            Hint = "The name of the action button to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string ActionName { get; set; }

        /// <summary>
        /// The label of the action button to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Action Label",
            Hint = "The label of the action button to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string ActionLabel { get; set; }

        /// <summary>
        /// The Default Value of the action input to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Action Input Default Value",
            Hint = "The Default Value of the action input to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript }
        )]
        public string ActionInputDefaultValue { get; set; }

        /// <summary>
        /// The description of the action to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Action Description",
            Hint = "The description of the action to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string ActionDescription { get; set; }

        /// <summary>
        /// The class of the action button to show at Cases Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Action Class",
            Hint = "The class of the action button to show at Cases Back-office UI.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string ActionClass { get; set; }

        /// <summary>
        /// Determines whether at the end of the action the user will be redirected to Cases list of Back-office UI.
        /// </summary>
        [ActivityInput(
            Label = "Redirect to List",
            Hint = "Determines whether at the end of the action the user will be redirected to Cases list of Back-office UI."
        )]
        public bool RedirectToList { get; set; }

        /// <summary>
        /// A response message that is returned if the action is completed with success.
        /// </summary>
        [ActivityInput(
            Label = "Success Message",
            Hint = "A response message that is returned if the action is completed with success.",
            UIHint = ActivityInputUIHints.MultiLine,
            DefaultSyntax = SyntaxNames.JavaScript,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public SuccessMessage SuccessMessage { get; set; }

        /// <summary>
        /// User role that can proceed to this action. If left blank, all authenticated users can proceed to this action.
        /// </summary>
        [ActivityInput(
            Label = "Role",
            Hint = "User role that can proceed to this action. If left blank, all authenticated users can proceed to this action.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string AllowedRole { get; set; } = string.Empty;

        /// <summary>
        /// Determines whether the Back-Office UI will have an input element.
        /// </summary>
        [ActivityInput(
            Label = "Show Input to Back-Office UI",
            Hint = "Show an Input field to Back-Office UI and send the value to the output of this activity."
        )]
        public bool ShowInput { get; set; }

        /// <summary>
        /// The output probably
        /// </summary>
        [ActivityOutput]
        public object Output { get; set; }

        /// <inheritdoc />
        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternal(context) : Suspend();
        }

        /// <inheritdoc />
        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
            return await OnExecuteInternal(context);
        }

        private async Task<IActivityExecutionResult> OnExecuteInternal(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);

            var input = context.Input as ActionRequest;

            Output = input?.Value ?? string.Empty;
            context.LogOutputProperty(this, nameof(Output), Output);

            var comment = $"Action \"{ActionName}\" executed successfully";
            await _caseMessageService.Send(CaseId.Value, context.TryGetUser()!, new Message {
                Comment = string.IsNullOrEmpty(input?.Value) ? $"{comment}." : $"{comment} with value \"{Output}\".",
                PrivateComment = true
            });

            return Done(Output);
        }
    }
}