using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Security;

namespace Indice.Features.Cases.Server.Integration;

public class WorkflowHttpServiceClient : ICasesWorkflowManager
{
    private readonly WorkflowHttpClient _workflowApiClient;

    public WorkflowHttpServiceClient(IHttpClientFactory factory) {
        var httpClient = factory.CreateClient(nameof(WorkflowHttpServiceClient)) ?? throw new ArgumentNullException(nameof(WorkflowHttpServiceClient));
        httpClient.BaseAddress = new Uri("https://localhost:2001/");
        _workflowApiClient = new WorkflowHttpClient(httpClient);
    }
    
    public async Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode) {
        try {
            await _workflowApiClient.StartWorkflowAsync(caseId, caseTypeCode);
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    public async Task<WorkflowInvocationResult> InvokeApprovalAsync(ClaimsPrincipal user, Guid caseId, ApprovalRequest request) {
        try {
            // todo: remove userRoles
            var userRoles = user
                .FindAll(x => x.Type == BasicClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();
            userRoles.Add(string.Empty);
            await _workflowApiClient.ApprovalAsync(new WorkflowSubmitApprovalRequest {
                CaseId = caseId,
                CasesUser = user.ToCasesUser(),
                OutputAction = Enum.Parse<Approval>(request.Action.ToString()),
                OutputComment = request.Comment,
                Roles = userRoles
            });
            return new WorkflowInvocationResult(true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }

    public async Task<WorkflowInvocationResult> InvokeEditAsync(ClaimsPrincipal user, Guid caseId, EditCaseRequest request) {
        try {
            await _workflowApiClient.EditAsync(new WorkflowEditCaseRequest {
                CaseId = caseId,
                Data = request.Data,
                CasesUser = user.ToCasesUser(),
            });
            return new WorkflowInvocationResult(Success: true, [], string.Empty);
        } catch (WorkflowApiException ex) {
            return new WorkflowInvocationResult(Success: false, [], ex.Message);
        }
    }
    
    public async Task<object> GetActionsByCaseId(ClaimsPrincipal user, Guid caseId, string[] roles) {
        try {
            // todo: remove userRoles
            var userRoles = user
                .FindAll(x => x.Type == BasicClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();
            userRoles.Add(string.Empty);
            return await _workflowApiClient.ActionsAsync(caseId, userRoles);
        } catch (WorkflowApiException ex) {
            return Task.FromResult<object>(new AvailableActions());
        }
    }

    public Task<WorkflowInvocationResult> AssignCaseAsync(ClaimsPrincipal user, Guid caseId) {
        throw new NotImplementedException();
    }

    public Task<WorkflowInvocationResult> TriggerActionAsync(ClaimsPrincipal user, Guid caseId, ActionRequest request) {
        throw new NotImplementedException();
    }

    public Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(ClaimsPrincipal user, Guid caseId) {
        return Task.FromResult(new List<RejectReason>());
    }

    // todo: this will become obsolete
    public async Task<CaseActions> GetAvailableActionsAsync(ClaimsPrincipal user, Guid caseId, string? assignedToId, string[] bookmarks, string? lastApprovedById = null) {
        try {
            var response = await _workflowApiClient.AvailableActionsAsync(caseId, assignedToId, bookmarks, user.FindSubjectId(), user.IsAdmin(), user.IsSystemClient(), lastApprovedById);
            return new CaseActions {
                HasAssignment = response.HasAssignment,
                HasApproval = response.HasApproval,
                HasUnassignment = response.HasUnassignment,
                HasEdit = response.HasEdit,
                CustomActions = response.CustomActions?.Select(x => new CustomCaseAction {
                    Id = x?.Id,
                    Description = x?.Description,
                    DefaultValue = x?.DefaultValue,
                    RedirectToList = x?.RedirectToList,
                    SuccessMessage = new SuccessMessage {
                        Body = x?.SuccessMessage?.Body ?? string.Empty,
                        Title = x?.SuccessMessage?.Title ?? string.Empty
                    },
                    Class = x.Class,
                    HasInput = x?.HasInput,
                    Label = x?.Label,
                    Name = x?.Name
                })?.ToList() ?? new List<CustomCaseAction>()
            };
        } catch (WorkflowApiException ex) {
            return new CaseActions();
        }
    }
}