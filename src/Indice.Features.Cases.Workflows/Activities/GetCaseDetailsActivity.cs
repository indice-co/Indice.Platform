using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integration;
using CustomOutcomeNames = Indice.Features.Cases.Workflows.CasesWorkflowConstants.WorkflowVariables.OutcomeNames;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Get Case Details",
    Description = "Get the details of the case.",
    Outcomes = new[] { OutcomeNames.Done, CustomOutcomeNames.Failed }
)]
internal class GetCaseDetailsActivity(CasesHttpClient casesHttpClient) : BaseCaseActivity(casesHttpClient)
{
    [ActivityOutput]
    public object? Output { get; set; }

    [ActivityInput(
        Label = "Include attachment binary data",
        Hint = "Use this with caution. Large binary data could break the instance."
    )]
    public bool IncludeAttachmentsData { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        var @case = await CasesClient.GetCaseAsync(CaseId.Value, IncludeAttachmentsData);
        
        // Convert CaseData to JObject so the workflow activities can use data without parsing.
        // @case.Data = Newtonsoft.Json.Linq.JObject.Parse(@case.DataAs<string?>()!); // todo: see if needed, simple activity first getCaseDetails, then set variable to check
        Output = @case;

        // context.SetVariable("tete", @case.Data);
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}
