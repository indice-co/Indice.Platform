using System.Text.Json;
using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;
using Indice.Serialization;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Get Case Details",
    Description = "Get the details of the case.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
)]
internal class GetCaseDetailsActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    [ActivityOutput]
    public object? Output { get; set; }

    [ActivityInput(
        Label = "Include attachment binary data",
        Hint = "Use this with caution. Large binary data could break the instance."
    )]
    public bool IncludeAttachmentsData { get; set; }

    [ActivityInput(
        Label = "Fetch Public Data",
        Hint = "Fetching of Public Data will affect all activities down the line using this activity's output."
    )]
    public bool FetchPublicData { get; set; } = false;

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var @case = await CasesManager.GetCaseById(CaseId.Value, FetchPublicData, IncludeAttachmentsData);
        
        // When trying to access Output object from another activity, we need NewtonSoft for Jint and Liquid evaluators to correctly operate on the data
        @case.Data = Newtonsoft.Json.Linq.JObject.Parse(JsonSerializer.Serialize(@case.Data, JsonSerializerOptionDefaults.GetDefaultSettings()));
        Output = @case;

        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}
