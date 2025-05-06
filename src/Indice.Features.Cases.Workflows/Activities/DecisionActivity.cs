using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Decision Activity",
    Description = "Get the details of the case.",
    Outcomes = new[] { "10", "20", "25", "30", OutcomeNames.False }
)]
internal class DecisionActivity(ICasesManager casesManager, CasesManagerHttpClient client) : BaseCaseActivity(casesManager)
{
    [ActivityOutput]
    public string? Output { get; set; }
    
    [ActivityInput(
        Label = "The name of the rule"
    )]
    public string RuleName { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var @case = await CasesManager.GetCaseById(CaseId.Value);
        var result = await client.RunWorkflowAsync(@case.CaseType.Code, RuleName, [@case.Data]);
        Output = result;
        return Outcome(result);
    }
}
