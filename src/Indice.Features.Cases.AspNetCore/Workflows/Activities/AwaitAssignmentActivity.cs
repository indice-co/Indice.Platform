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

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>
/// A blocking activity that awaits signal from client.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
[Trigger(
    Category = "Cases",
    DisplayName = "Await Assignment",
    Description = "When a user triggers this activity, they will assign the current workflow case to themselves.",
    Outcomes = new[] { OutcomeNames.Done, CasesApiConstants.WorkflowVariables.OutcomeNames.Failed }
)]
internal class AwaitAssignmentActivity : BaseCaseActivity
{
    private readonly IAdminCaseService _adminCaseService;

    public AwaitAssignmentActivity(
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

    [ActivityInput(
    Label = "SelfAssign",
    Hint = "SelfAssign."
    )]
    public bool SelfAssign { get; set; }

    [ActivityInput(
    Label = "Assign",
    Hint = "Assign."
    )]
    public bool Assign { get; set; }

    [ActivityOutput]
    public AuditMeta Output { get; set; }
    [ActivityOutput]
    public bool WasSelfAssign { get; set; }

    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) {
        return context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternal(context) : Suspend();
    }

    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) {
        return await OnExecuteInternal(context);
    }

    private async Task<IActivityExecutionResult> OnExecuteInternal(ActivityExecutionContext context) {
        CaseId ??= Guid.Parse(context.CorrelationId);
        // Read received input.
        var receivedInput = context.GetInput<AwaitAssignmentInvokerInput>();
        AuditMeta assignedTo;
        try {
            assignedTo = receivedInput.SelfAssign ?
                await _adminCaseService.AssignCase(AuditMeta.Create(context.GetHttpContextUser()), CaseId!.Value) :
                await _adminCaseService.AssignCase(receivedInput.User, CaseId!.Value);
        } catch (Exception ex) {
            await LogCaseError(context, ex);
            return Outcome("Failed");
        }

        Output = assignedTo;
        WasSelfAssign = receivedInput.SelfAssign;
        context.LogOutputProperty(this, "Output", Output);
        return Done();
    }
}