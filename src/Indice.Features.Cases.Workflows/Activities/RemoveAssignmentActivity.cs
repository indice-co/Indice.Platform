using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Clear the assignedTo property for a Case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Remove Assignment from user",
    Description = "Remove the assignment of a case.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class RemoveAssignmentActivity : BaseCaseActivity
{
    private readonly CasesHttpClient _casesClient;

    public RemoveAssignmentActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient ?? throw new ArgumentNullException(nameof(casesClient));
    }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // await _casesClient.RemoveAssignmentAsync(CaseId.Value);
        // await _adminCaseService.RemoveAssignment(CaseId!.Value);
        return Done();
    }
}