using Elsa;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services.Models;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Extensions;
using Newtonsoft.Json;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// A blocking activity that awaits signal from client.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
[Trigger(
    Category = "Cases",
    DisplayName = "Await Assignment vol2",
    Description = "When a user triggers this activity, they ...",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class AwaitAssignmentVol2Activity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;
    
    public AwaitAssignmentVol2Activity(
        IAdminCaseMessageService caseMessageService,
        IAdminCaseService adminCaseService)
        : base(caseMessageService) {
        _adminCaseService = adminCaseService ?? throw new ArgumentNullException(nameof(adminCaseService));
    }

    [ActivityInput(
        Label = "Role",
        Hint = "User role that can assign a case to self. If left blank, any authenticated user can assign a case to them.",
        UIHint = ActivityInputUIHints.SingleLine,
        DefaultSyntax = SyntaxNames.Literal,
        SupportedSyntaxes = new[] { SyntaxNames.Literal }
    )]
    public string AllowedRole { get; set; }

    [ActivityOutput]
    public AuditMeta Output { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternal(context) : Suspend();
    }

    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
        return await OnExecuteInternal(context);
    }

    private async Task<IActivityExecutionResult> OnExecuteInternal(ActivityExecutionContext context) {
        // Read received input.
        var receivedInput = context.GetInput<AwaitAssignmentInvokerInput>();
        //var b = context.Input.User.ToString();
        //var a = JsonConvert.DeserializeObject<AuditMeta>(b);
        CaseId ??= Guid.Parse(context.CorrelationId);
        AuditMeta assignedTo;
        try {
            assignedTo = await _adminCaseService.AssignCase(receivedInput.User, CaseId!.Value);
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome("Failed");
        }

        Output = assignedTo;
        context.LogOutputProperty(this, "Output", Output);
        return Done();
    }
}