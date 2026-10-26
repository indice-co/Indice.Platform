using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Get the channel of the case. Legacy channel names are returned with their current name (Agent as Managed, Customer as External).</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Get Channel",
    Description = "Get the channel of the case. Cases stored as Agent/Customer return Managed/External.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class GetChannelActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var @case = await CasesManager.GetCaseById(CaseId.Value);
        Output = CasesWorkflowConstants.Channels.Normalize(@case.Channel)!;
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}