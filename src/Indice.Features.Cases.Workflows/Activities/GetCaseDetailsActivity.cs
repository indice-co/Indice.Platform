using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Integration;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Get Case Details",
    Description = "Get the details of the case.",
    Outcomes = new[] { OutcomeNames.Done, CasesWorkflowConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class GetCaseDetailsActivity : BaseCaseActivity
{
    private CasesHttpClient _casesClient;

    public GetCaseDetailsActivity(CasesHttpClient casesClient) : base(casesClient) {
        _casesClient = casesClient;
    }

    [ActivityOutput]
    public object? Output { get; set; }

    [ActivityInput(
        Label = "Include attachment binary data",
        Hint = "Use this with caution. Large binary data could break the instance."
    )]
    public bool IncludeAttachmentsData { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // Run as systemic user, since this is a system activity for creating conditions at workflow
        var systemUser = CasesClaimsPrincipalExtensions.SystemUser();
        // var @case = await _adminCaseService.GetCaseById(systemUser, CaseId.Value, IncludeAttachmentsData);
        
        var @case = await _casesClient.GetCaseByIdAsync(CaseId.Value, IncludeAttachmentsData);
        
        // Convert CaseData to JObject so the workflow activities can use data without parsing.
        //@case.Data = Newtonsoft.Json.Linq.JObject.Parse(@case.DataAs<string?>()!);
        Output = @case; 
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}
