using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
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
    Description = "Assign the case to a back-office user.",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
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

    [ActivityInput(
        Label = "User to assign",
        Hint = "The AuditMeta object of the user to assign the case",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript }
    )]
    public AuditMeta AssignTo { get; set; } = new();

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        await _adminCaseService.AssignCase(AssignTo, CaseId.Value);
        return Done();
    }
}