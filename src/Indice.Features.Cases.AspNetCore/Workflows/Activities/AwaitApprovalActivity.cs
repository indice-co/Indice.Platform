using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Resources;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Security;

namespace Indice.Features.Cases.Workflows.Activities
{
    /// <summary>
    /// A blocking activity that awaits signal from client.
    /// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
    /// </summary>
    [Trigger(
        Category = "Cases - Approvals",
        DisplayName = "Await Approval",
        Description = "Handles the approval or rejection of a case.",
        Outcomes = new[] { nameof(Approval.Approve), nameof(Approval.Reject) }
    )]
    internal class AwaitApprovalActivity : BaseCaseActivity
    {
        private readonly IAdminCaseMessageService _caseMessageService;
        private readonly ICaseApprovalService _caseApprovalService;
        private readonly CaseSharedResourceService _caseSharedResourceService;

        public AwaitApprovalActivity(
            IAdminCaseMessageService caseMessageService,
            ICaseApprovalService caseApprovalService,
            CaseSharedResourceService caseSharedResourceService)
            : base(caseMessageService) {
            _caseMessageService = caseMessageService;
            _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
            _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
        }

        [ActivityInput(
            Label = "Role",
            Hint = "Admin user role that can provide approval. If left blank, all authenticated users can approve/reject.",
            UIHint = ActivityInputUIHints.SingleLine,
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal }
        )]
        public string AllowedRole { get; set; } = string.Empty;

        [ActivityInput(
            Label = "Block previous approver",
            Hint = "Check this to block approvals from the same user."
        )]
        public bool BlockPreviousApprover { get; set; }

        [ActivityInput(
            Label = "Send approval comment to customer (if any)",
            Hint = "Show the approval comment of the selected actions to the customer or front-end of the application.",
            Options = new[] { nameof(Approval.Approve), nameof(Approval.Reject) },
            UIHint = ActivityInputUIHints.CheckList,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript }
        )]
        public IEnumerable<string> PublicActions { get; set; } = new List<string>();

        [ActivityOutput]
        public ApprovalRequest Output { get; set; }

        [ActivityOutput]
        public string Action { get; set; }

        public override async ValueTask<bool> CanExecuteAsync(ActivityExecutionContext context) {
            var user = context.TryGetUser()!;
            // If the activity input is null or the user has the claim "Administrator" (as configured at Indice.Security)
            // allow the execution
            if (string.IsNullOrEmpty(AllowedRole) || user.IsAdmin()) {
                return true;
            }
            // User must be in allowed role to continue executing
            return user.IsInRole(AllowedRole) && await UserCanApprove(context);
        }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            // Since we are writing a blocking activity, the activity needs to tell the workflow engine that execution should pause until an ApprovalRequest is received.
            // That will work, but only when the activity is used a blocking activity and not as a starting activity. If we used this as a starting activity,
            // what will happen is that when an ApprovalRequest is received, the workflow will begin, but gets suspended immediately after. That's no good.
            // Instead, what we want is for the workflow to continue to the next activity when an ApprovalRequest is received.
            // To make that work, we need to return a SuspendResult only if this is not the first pass.If it IS the first pass, we will simply return an OutcomeResult with the "Done" outcome.
            return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternalAsync(context) : Suspend();
        }

        protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
            // That will achieve exactly what we need: when the activity is used as a starting activity, it will return "Done" and execution of the workflow will continue.
            // But when the activity is used as a blocking activity (i.e. not as the first activity of the workflow), the activity will suspend the workflow.           
            // The big idea is that we should be able to trigger workflows when an ApprovalRequest is received, regardless of whether we have workflows that use this as a starting trigger
            // or as a trigger to resume existing workflow instances.
            return await OnExecuteInternalAsync(context);
        }

        private async Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context) {
            // Get approval from activity input trigger
            var approval = context.Input as ApprovalRequest;

            CaseId ??= Guid.Parse(context.CorrelationId);

            // Set activity's output properties 
            Output = approval;
            Action = approval!.Action.ToString();
            context.LogOutputProperty(this, "Output", Output);

            // Send a message to the case service regarding the approval action. If PublicActions property contains the action,
            // then make the comment public 
            await _caseMessageService.Send(
                CaseId!.Value,
                context.TryGetUser()!,
                new Message {
                    Comment = _caseSharedResourceService.GetLocalizedHtmlString(approval.Comment ?? string.Empty).Value,
                    PrivateComment = !PublicActions.Contains(approval.Action.ToString())
                });

            await _caseApprovalService.AddApproval(CaseId!.Value, null, context.TryGetUser()!, approval.Action, approval.Comment);

            return Outcome(approval.Action.ToString(), approval);
        }

        private async ValueTask<bool> UserCanApprove(ActivityExecutionContext context) {
            if (!BlockPreviousApprover) {
                return true;
            }

            var caseId = CaseId ??= Guid.Parse(context.CorrelationId);
            var lastApproval = await _caseApprovalService.GetLastApproval(caseId);
            return context.TryGetUser()!.FindSubjectId() != lastApproval?.CreatedBy.Id;
        }
    }
}
