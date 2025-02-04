using System.Globalization;
using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Localization;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Integration;

public class WorkflowHttpServiceClient : ICasesWorkflowManager
{
    private readonly WorkflowHttpClient _workflowApiClient;
    private readonly CasesOptions _casesOptions;
    private readonly CaseSharedResourceService _caseSharedResourceService;

    /// <summary>
    /// 
    /// </summary>
    public WorkflowHttpServiceClient(IHttpClientFactory factory, IOptions<CasesOptions> caseOptions, CaseSharedResourceService caseSharedResourceService) {
        var httpClient = factory.CreateClient(nameof(WorkflowHttpServiceClient)) ?? throw new ArgumentNullException(nameof(WorkflowHttpServiceClient));
        httpClient.BaseAddress = new Uri("https://localhost:2001/"); // todo: from config
        _workflowApiClient = new WorkflowHttpClient(httpClient);
        _casesOptions = caseOptions.Value ?? throw new ArgumentNullException(nameof(caseOptions));
        _caseSharedResourceService = caseSharedResourceService ?? throw new ArgumentNullException(nameof(caseSharedResourceService));
    }

    // todo: problem details
    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode, AuditMeta auditMeta) {
        try {
            // todo: pass reference id
            await _workflowApiClient.StartWorkflowAsync(caseId, caseTypeCode, new Actor {
                Name = auditMeta.Name,
                UserId = auditMeta.Id,
                Email = auditMeta.Email,
            });
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeAssignmentAsync(Guid caseId, ClaimsPrincipal user) {
        try {
            await _workflowApiClient.AssignAsync(new InvokeAssignmentRequest {
                CaseId = caseId,
                Actor = user.ToWorkflowActor(_casesOptions),
            });
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeApprovalAsync(ClaimsPrincipal user, Guid caseId, ApprovalRequest request) {
        try {
            await _workflowApiClient.ApprovalAsync(new InvokeApprovalRequest {
                CaseId = caseId,
                Action = Enum.Parse<WorkflowApproval>(request.Action.ToString()),
                Comment = request.Comment,
                Actor = user.ToWorkflowActor(_casesOptions)
            });
            return new WorkflowInvocationResult(true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> InvokeEditAsync(ClaimsPrincipal user, Guid caseId, string? comment, EditCaseRequest request) {
        try {
            await _workflowApiClient.EditAsync(new InvokeEditRequest {
                CaseId = caseId,
                Data = request.Data,
                Actor = user.ToWorkflowActor(_casesOptions),
            });
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<IWorkflowActions> GetActionsByCaseId(Guid caseId) { // todo: correct return
        try {
            return await _workflowApiClient.ActionsAsync(caseId);
        } catch (WorkflowApiException ex) {
            return new AvailableActions();
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowInvocationResult> TriggerActionAsync(ClaimsPrincipal user, Guid caseId, ActionRequest request) {
        try {
            await _workflowApiClient.ActionAsync(new InvokeActionRequest {
                CaseId = caseId,
                ActionId = request.Id,
                Value = request.Value,
                Actor = user.ToWorkflowActor(_casesOptions)
            });
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(ClaimsPrincipal user, Guid caseId) {
        try {
            var reasons = await _workflowApiClient.RejectReasonsAsync(caseId);
            return reasons.Select(reason => new RejectReason {
                Key = reason,
                Value = _caseSharedResourceService.GetLocalizedHtmlString(reason, CultureInfo.CurrentCulture.Name).Value
            }).ToList();
        } catch (WorkflowApiException ex) {
            return [];
        }
    }

    // todo: this will become obsolete
    /// <inheritdoc />
    public async Task<CaseActions> GetAvailableActionsAsync(ClaimsPrincipal user, Guid caseId, string? assignedToId, string[] bookmarks, string? lastApprovedById = null) {
        throw new NotImplementedException();
    }
}