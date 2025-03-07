using Elsa.Services.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAction;

namespace Indice.Features.Cases.Workflows.Interfaces;

/// <summary>
/// Invoker for <see cref="AwaitActionBookmark"/> that triggers the continuation of a resumed <see cref="AwaitActionActivity"/>.
/// <remarks>See: <a href="https://elsa-workflows.github.io/elsa-core/docs/next/guides/guides-blocking-activities">Elsa Blocking Activities</a></remarks>
/// </summary>
internal interface IAwaitActionInvoker
{
    /// <summary>
    /// Dispatching a workflow will not execute the workflow directly, but instead send an instruction to a message queue.
    /// A background worker will process this queue, and therefore, execute the workflow in the background.
    /// </summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The action request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, ActionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Executing a workflow will execute the workflow directly before returning.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The action request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, ActionRequest request, CancellationToken cancellationToken = default);
}