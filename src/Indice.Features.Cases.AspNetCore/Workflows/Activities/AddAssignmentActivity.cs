using System.Security.Claims;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// Add the assignedTo property for a Case.
/// </summary>
[Activity(
    Category = "Cases",
    DisplayName = "Add Assignment",
    Description = "Assign a back-office user a case.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class AddAssignmentActivity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;

    public AddAssignmentActivity(
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService)
        : base(caseMessageService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    public AuditMeta User { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);

        var claimsPrincipal = Indice.Features.Cases.Extensions.PrincipalExtensions.FromCase()

        await _adminCaseService.AssignCase(null, CaseId.Value);

        return Done();
    }
}