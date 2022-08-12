using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using IdentityModel;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Services
{
    internal class AwaitApprovalInvoker : BaseActivityInvoker, IAwaitApprovalInvoker
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AwaitApprovalInvoker(
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore,
            IHttpContextAccessor httpContextAccessor)
            : base(workflowLaunchpad, workflowInstanceStore) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, ApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
            base.DispatchWorkflowsAsync(caseId, approvalRequest, cancellationToken);

        public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, ApprovalRequest approvalRequest, CancellationToken cancellationToken = default) =>
            base.ExecuteWorkflowsAsync(caseId, approvalRequest, cancellationToken);

        protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) {
            var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);
            
            var userRoles = _httpContextAccessor.HttpContext.User
                .FindAll(x => x.Type == JwtClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();

            // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
            userRoles.Add(string.Empty);

            return userRoles.Select(role => new WorkflowsQuery(
                nameof(AwaitApprovalActivity),
                new AwaitApprovalBookmark(caseId.ToString(), role),
                caseId.ToString(),
                instance.Id));
        }
    }
}