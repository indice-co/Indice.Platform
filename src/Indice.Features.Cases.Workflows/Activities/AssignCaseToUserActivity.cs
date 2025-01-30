using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Add the assignedTo property for a Case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Assign case to user",
    Description = "Assign the case to a back-office user.",
    Outcomes = new[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class AssignCaseToUserActivity : BaseCaseActivity
{
    // private readonly IAdminCaseService _adminCaseService;
    private readonly CasesHttpClient _casesClient;

    public AssignCaseToUserActivity(CasesHttpClient casesClient)
        : base(casesClient) {
        // _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
        _casesClient = casesClient ?? throw new ArgumentNullException(nameof(casesClient));
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
            // await _casesClient.AssignCaseAsync(CaseId.Value);
            // await _adminCaseService.AssignCase(User, CaseId.Value);
            await _casesClient.AssignCaseAsync(CaseId.Value); //  todo: pass AuditMeta here
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome("Failed");
        }
        return Done();
    }
}