using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;

namespace Indice.Features.Cases.Workflows.Services
{
    internal abstract class BaseActivityInvoker
    {
        private readonly IWorkflowLaunchpad _workflowLaunchpad;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;

        protected BaseActivityInvoker(
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore) {
            _workflowLaunchpad = workflowLaunchpad ?? throw new ArgumentNullException(nameof(workflowLaunchpad));
            _workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
        }

        /// <summary>
        /// Dispatching a workflow will not execute the workflow directly, but instead send an instruction to a message queue.
        /// A background worker will process this queue, and therefore, execute the workflow in the background.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="input">The input data for the activity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync<TWorkflowInput>(Guid caseId, TWorkflowInput input, CancellationToken cancellationToken = default) {
            var queries = (await CreateWorkflowsQueries(caseId, cancellationToken)).ToList();

            // Loop until a workflow has been dispatched. Then break the loop (Avoid multiple dispatches)
            var collectedWorkflows = new List<CollectedWorkflow>();
            foreach (var query in queries.TakeWhile(query => !collectedWorkflows.Any())) {
                collectedWorkflows.AddRange(await _workflowLaunchpad.CollectAndDispatchWorkflowsAsync(query, new WorkflowInput(input), cancellationToken));
            }
            return collectedWorkflows;
        }

        /// <summary>
        /// Executing a workflow will execute the workflow directly before returning.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="input">The input data for the activity.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync<TWorkflowInput>(Guid caseId, TWorkflowInput input, CancellationToken cancellationToken = default) {
            var queries = (await CreateWorkflowsQueries(caseId, cancellationToken)).ToList();

            // Loop until a workflow has been executed. Then break the loop (Avoid multiple executions)
            var collectedWorkflows = new List<CollectedWorkflow>();
            foreach (var query in queries.TakeWhile(query => !collectedWorkflows.Any()))
            {
                collectedWorkflows.AddRange(await _workflowLaunchpad.CollectAndExecuteWorkflowsAsync(query, new WorkflowInput(input), cancellationToken));
            }
            return collectedWorkflows;
        }

        /// <summary>
        /// Create the Workflow Query based on each bookmark's logic.
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        protected abstract Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the Workflow instance where CorrelationId is the CaseId
        /// </summary>
        /// <param name="caseId">The Id of the case.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        protected async Task<WorkflowInstance> GetWorkflowInstanceByCaseId(Guid caseId, CancellationToken cancellationToken = default)
            => await _workflowInstanceStore.FindByCorrelationIdAsync(caseId.ToString(), cancellationToken)
               ?? throw new Exception($"No workflow instances found for CaseId {caseId}.");
    }
}