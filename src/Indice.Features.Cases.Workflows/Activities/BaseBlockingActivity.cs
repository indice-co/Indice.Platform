using Elsa.ActivityResults;
using Elsa.Services.Models;
using Indice.Features.Cases.Workflows.Integrations;

namespace Indice.Features.Cases.Workflows.Activities;

/// <summary>Base Blocking Activity for Cases which provides automatic suspending of action on execute or automatic run when this is the first activity.</summary>
public abstract class BaseBlockingActivity(ICasesManager casesManager) : BaseCaseActivity(casesManager)
{
    /// <summary>
    /// Since we are writing a blocking activity, the activity needs to tell the workflow engine that execution should pause until an Request is received.
    /// That will work, but only when the activity is used a blocking activity and not as a starting activity. If we used this as a starting activity,
    /// what will happen is that when an ApprovalRequest is received, the workflow will begin, but gets suspended immediately after. That's no good.
    /// Instead, what we want is for the workflow to continue to the next activity when an ApprovalRequest is received.
    /// To make that work, we need to return a SuspendResult only if this is not the first pass.If it IS the first pass, we will simply return an OutcomeResult with the "Done" outcome.
    /// </summary>
    public override async ValueTask<IActivityExecutionResult> TryExecuteAsync(ActivityExecutionContext context) => 
        context.WorkflowExecutionContext.IsFirstPass ? await OnExecuteInternalAsync(context) : Suspend();

    /// <summary>
    /// That will achieve exactly what we need: when the activity is used as a starting activity, it will return "Done" and execution of the workflow will continue.
    /// But when the activity is used as a blocking activity (i.e. not as the first activity of the workflow), the activity will suspend the workflow.           
    /// The big idea is that we should be able to trigger workflows when an ApprovalRequest is received, regardless of whether we have workflows that use this as a starting trigger
    /// or as a trigger to resume existing workflow instances.
    /// </summary>
    protected override async ValueTask<IActivityExecutionResult> OnResumeAsync(ActivityExecutionContext context) => 
        await OnExecuteInternalAsync(context);

    /// <summary>Activity Execution Logic.</summary>
    protected abstract Task<IActivityExecutionResult> OnExecuteInternalAsync(ActivityExecutionContext context);
}