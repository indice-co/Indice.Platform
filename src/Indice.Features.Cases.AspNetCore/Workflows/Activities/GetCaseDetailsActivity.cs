using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities;

[Activity(
    Category = "Cases",
    DisplayName = "Get Case Details",
    Description = "Get the details of the case.",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class GetCaseDetailsActivity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;

    public GetCaseDetailsActivity(
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService)
        : base(caseMessageService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    [ActivityOutput]
    public object Output { get; set; }

    [ActivityInput(
        Label = "Include attachment binary data",
        Hint = "Use this with caution. Large binary data could break the instance."
    )]
    public bool IncludeAttachmentsData { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // Run as systemic user, since this is a system activity for creating conditions at workflow
        var systemUser = Cases.Extensions.PrincipalExtensions.SystemUser();
        var @case = await _adminCaseService.GetCaseById(systemUser, CaseId.Value, IncludeAttachmentsData);
        
        // Convert CaseData to JObject so the workflow activities can use data without parsing.
        @case.Data = Newtonsoft.Json.Linq.JObject.Parse(@case.DataAs<string>());
        Output = @case;
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}
