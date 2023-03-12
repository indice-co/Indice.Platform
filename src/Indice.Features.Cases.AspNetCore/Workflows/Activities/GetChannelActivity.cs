using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services.Models;
using Indice.Features.Cases.Interfaces;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Get the channel of the case.</summary>
[Activity(
    Category = "Cases",
    DisplayName = "Get Channel",
    Description = "Get the channel of the case.",
    Outcomes = new[] { OutcomeNames.Done }
)]
internal class GetChannelActivity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;

    public GetChannelActivity(
        IAdminCaseMessageService caseMessageService, 
        IAdminCaseService adminCaseService)
        : base(caseMessageService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }
    
    [ActivityOutput]
    public object Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // Run as systemic user, since this is a system activity for creating conditions at workflow
        var systemUser = Cases.Extensions.PrincipalExtensions.SystemUser();
        var @case = await _adminCaseService.GetCaseById(systemUser, CaseId.Value!);
        Output = @case.Channel!;
        context.LogOutputProperty(this, nameof(Output), Output);
        return Done(Output);
    }
}