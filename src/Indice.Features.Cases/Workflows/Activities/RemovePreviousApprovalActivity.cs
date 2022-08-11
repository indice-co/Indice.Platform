using System;
using System.Threading.Tasks;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities
{
    [Activity(
        Category = "Cases - Approvals",
        DisplayName = "Remove previous approval",
        Description = "Remove the previous approval action.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    internal class RemovePreviousApprovalActivity : BaseCaseActivity
    {
        private readonly ICaseApprovalService _caseApprovalService;

        public RemovePreviousApprovalActivity(
            IAdminCaseMessageService caseMessageService,
            ICaseApprovalService caseApprovalService) : base(caseMessageService) {
            _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);
            await _caseApprovalService.RollbackApproval(CaseId.Value);
            return Done();
        }
    }
}