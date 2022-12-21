using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities
{
    /// <summary>
    /// Clear the assignedTo property for a Case.
    /// </summary>
    [Activity(
        Category = "Cases",
        DisplayName = "Remove Assignment",
        Description = "Remove the assignment of a case.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    internal class RemoveAssignmentActivity : BaseCaseActivity
    {
        private readonly IAdminCaseService _adminCaseService;

        public RemoveAssignmentActivity(
            IAdminCaseMessageService caseMessageService, 
            IAdminCaseService adminCaseService)
            : base(caseMessageService) {
            _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        }

        public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
            CaseId ??= Guid.Parse(context.CorrelationId);
            await _adminCaseService.RemoveAssignment(CaseId!.Value);
            return Done();
        }
    }
}