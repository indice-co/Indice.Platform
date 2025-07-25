using System.Globalization;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Integration;

/// <inheritdoc />
public class WorkflowHttpServiceClient : ICasesWorkflowManager
{
    private readonly WorkflowHttpClient _workflowApiClient;
    private readonly CaseSharedResourceService _caseSharedResourceService;
    
    /// <summary>WorkflowHttpServiceClient Constructor</summary>
    public WorkflowHttpServiceClient(
        WorkflowHttpClient workflowApiClient,
        CaseSharedResourceService caseSharedResourceService) {
        _workflowApiClient = workflowApiClient ?? throw new ArgumentNullException(nameof(workflowApiClient));
        _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode, UserActor workflowActor) {
        try {
            await _workflowApiClient.StartWorkflowAsync(caseId, caseTypeCode, workflowActor.ToActor());
            return new WorkflowInvocationResult(Success: true, []);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeAssignmentAsync(Guid caseId, UserActor user) {
        try {
            await _workflowApiClient.AssignAsync(new InvokeAssignmentRequest {
                CaseId = caseId,
                Actor = user.ToActor(),
            });
            return new WorkflowInvocationResult(Success: true, []);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeApprovalAsync(UserActor user, Guid caseId, ApprovalRequest request) {
        try {
            await _workflowApiClient.ApprovalAsync(new InvokeApprovalRequest {
                CaseId = caseId,
                Action = Enum.Parse<WorkflowApproval>(request.Action.ToString()),
                Comment = request.Comment,
                Actor = user.ToActor()
            });
            return new WorkflowInvocationResult(true, []);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeEditAsync(UserActor user, Guid caseId, string? comment, EditCaseRequest request) {
        try {
            await _workflowApiClient.EditAsync(new InvokeEditRequest {
                CaseId = caseId,
                Data = request.Data,
                Actor = user.ToActor(),
            });
            return new WorkflowInvocationResult(Success: true, []);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IWorkflowActions> GetActionsByCaseId(Guid caseId) {
        try {
            return await _workflowApiClient.ActionsAsync(caseId);
        } catch (WorkflowApiException) {
            return new AvailableActions();
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> TriggerActionAsync(UserActor user, Guid caseId, ActionRequest request) {
        try {
            await _workflowApiClient.ActionAsync(new InvokeActionRequest {
                CaseId = caseId,
                ActionId = request.Id,
                Value = request.Value,
                Actor = user.ToActor()
            });
            return new WorkflowInvocationResult(Success: true, []);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(UserActor user, Guid caseId) {
        try {
            var reasons = await _workflowApiClient.RejectReasonsAsync(caseId);
            return reasons.Select(reason => new RejectReason {
                Key = reason,
                Value = _caseSharedResourceService.GetLocalizedHtmlString(reason, CultureInfo.CurrentCulture.Name).Value
            }).ToList();
        } catch (WorkflowApiException) {
            return [];
        }
    }


    /// <inheritdoc />
    [Obsolete("This method is obsolete and will be removed in a future version.")]
    public Task<CaseActions> GetAvailableActionsAsync(UserActor user, Guid caseId, string? assignedToId, string[] bookmarks, string? lastApprovedById = null) {
        throw new NotImplementedException();
    }
}