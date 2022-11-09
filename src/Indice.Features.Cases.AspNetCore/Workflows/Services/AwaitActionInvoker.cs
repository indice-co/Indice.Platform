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
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAction;
using Indice.Features.Cases.Workflows.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Indice.Features.Cases.Workflows.Services
{
    internal class AwaitActionInvoker : BaseActivityInvoker, IAwaitActionInvoker
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <inheritdoc />
        public AwaitActionInvoker(
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore,
            IHttpContextAccessor httpContextAccessor)
            : base(workflowLaunchpad, workflowInstanceStore) {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Task<IEnumerable<CollectedWorkflow>> DispatchWorkflowsAsync(Guid caseId, CustomActionRequest actionId, CancellationToken cancellationToken = default) =>
            base.DispatchWorkflowsAsync(caseId, actionId, cancellationToken);

        public Task<IEnumerable<CollectedWorkflow>> ExecuteWorkflowsAsync(Guid caseId, CustomActionRequest actionId, CancellationToken cancellationToken = default) =>
            base.ExecuteWorkflowsAsync(caseId, actionId, cancellationToken);

        protected override Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries(Guid caseId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<WorkflowsQuery>()); // ignore this implementation

        protected override async Task<IEnumerable<WorkflowsQuery>> CreateWorkflowsQueries<TWorkflowInput>(Guid caseId, TWorkflowInput input,
            CancellationToken cancellationToken = default) {
            var instance = await GetWorkflowInstanceByCaseId(caseId, cancellationToken);

            var userRoles = _httpContextAccessor.HttpContext?.User
                .FindAll(x => x.Type == JwtClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();

            // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
            userRoles?.Add(string.Empty);

            var actionInput = input as CustomActionRequest;

            return userRoles?.Select(role => new WorkflowsQuery(
                nameof(AwaitActionActivity),
                new AwaitActionBookmark(caseId.ToString(), role, actionInput?.Id.ToString() ?? string.Empty),
                caseId.ToString(),
                instance.Id)) ?? Enumerable.Empty<WorkflowsQuery>();
        }
    }
}