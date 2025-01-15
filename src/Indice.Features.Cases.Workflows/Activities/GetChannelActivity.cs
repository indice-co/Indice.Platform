using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Get the channel of the case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Get Channel",
    Description = "Get the channel of the case.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class GetChannelActivity(CasesHttpClient casesHttpClient) : BaseCaseActivity(casesHttpClient)
{
    [ActivityOutput]
    public object? Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var @case = await CasesClient.GetCaseAsync(CaseId.Value, false);
        Output = @case.Channel!;
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
    
    
}