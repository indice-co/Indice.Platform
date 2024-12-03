using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Assign case to user",
    Description = "Assign the case to a back-office user.",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class AssignCaseToUserActivity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;

    public AssignCaseToUserActivity(
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService)
        : base(caseMessageService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    [ActivityInput(
        Label = "User",
        Hint = "The AuditMeta object of the user to assign the case",
        UIHint = ActivityInputUIHints.MultiLine,
        DefaultSyntax = SyntaxNames.JavaScript,
        SupportedSyntaxes = new[] { SyntaxNames.JavaScript }
    )]
    public AuditMeta User { get; set; } = new();

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        try {
            await _adminCaseService.AssignCase(User, CaseId.Value);
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome("Failed");
        }
        return Done();
    }
}