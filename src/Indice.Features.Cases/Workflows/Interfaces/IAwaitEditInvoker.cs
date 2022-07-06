using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Indice.Features.Cases.Workflows.Interfaces
{
    internal interface IAwaitEditInvoker
    {
        Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, string data, CancellationToken cancellationToken = default);
        Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, string data, CancellationToken cancellationToken = default);
    }
}