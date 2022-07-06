using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;
using Indice.Features.Cases.Models;

namespace Indice.Features.Cases.Workflows.Interfaces
{
    internal interface IAwaitAssignmentInvoker
    {
        Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, AwaitAssignmentInvokerInput input, CancellationToken cancellationToken = default);
        Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, AwaitAssignmentInvokerInput input, CancellationToken cancellationToken = default);
    }
}