using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;

namespace Indice.Features.Cases.Core;

/// <summary>
/// Case Workflow manager. Interfaces with the workflow subsystem.
/// </summary>
/// <remarks>Invoking workflow activities for suspended workflow instances.</remarks>
public interface ICasesWorkflowManager
{
    /// <summary>Invoke the approval activity to approve or reject the case.</summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The approval request.</param>
    /// <returns>The <see cref="WorkflowInvocationResult"/> indicating success or failure of the operation</returns>
    Task<WorkflowInvocationResult> InvokeApprovalAsync(UserActor user, Guid caseId, ApprovalRequest request);

    /// <summary>Invoke the assign activity to assign the case to the caller user.</summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>The <see cref="WorkflowInvocationResult"/> indicating success or failure of the operation</returns>
    Task<WorkflowInvocationResult> InvokeAssignmentAsync(Guid caseId, UserActor user);

    /// <summary>Invoke the edit activity to edit the data of the case.</summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The case data in json format.</param>
    /// <param name="comment">The comment added.</param>
    /// <returns>The <see cref="WorkflowInvocationResult"/> indicating success or failure of the operation</returns>
    Task<WorkflowInvocationResult> InvokeEditAsync(UserActor user, Guid caseId, string? comment, EditCaseRequest request);

    /// <summary>Invoke the action activity to trigger a business action for the case.</summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="request">The action request.</param>
    /// <returns>The <see cref="WorkflowInvocationResult"/> indicating success or failure of the operation</returns>
    Task<WorkflowInvocationResult> TriggerActionAsync(UserActor user, Guid caseId, ActionRequest request);

    /// <summary>Query the list of available reasons to select from indicating why an approval request for a case was rejected.</summary>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <returns>The list of reasons indicating why an approval request was rejected</returns>
    Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(UserActor user, Guid caseId);

    /// <summary>Gets the available trigger actions for the given <paramref name="caseId"/> according to the current user and a bookmark list.</summary>
    /// <remarks>Bookmarks are used to filter out actions depending on annotations that have been assigned on the </remarks>
    /// <param name="user">The current user</param>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="assignedToId">The user id assignment already on the current case</param>
    /// <param name="bookmarks">Any bookmarks to find (usualy the roles)</param>
    /// <param name="lastApprovedById">Last case approver userid if any</param>
    /// <returns></returns>
    Task<CaseActions> GetAvailableActionsAsync(UserActor user, Guid caseId, string? assignedToId, string[] bookmarks, string? lastApprovedById = null);

    /// <summary>Gets the available trigger actions for the given <paramref name="caseId"/> according to the current user and a bookmark list.</summary>
    /// <remarks>Bookmarks are used to filter out actions depending on annotations that have been assigned on the </remarks>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="caseTypeCode">The case type code. Will be used as a bookmark</param>
    /// <param name="workflowActor">The actor meta starting thr workflow.</param>
    /// <returns>The <see cref="WorkflowInvocationResult"/> indicating success or failure of the operation</returns>
    Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode, UserActor workflowActor);

    /// <summary>Gets the available trigger actions for the given <paramref name="caseId"/> regardless of the current user. Authorization happens on the cases host.</summary>
    /// <param name="caseId">The Id of the case.</param>
    Task<IWorkflowActions> GetActionsByCaseId(Guid caseId);
}

/// <summary>The interface that represents the list of available actions in the workflow.</summary>
public interface IWorkflowActions { }

/// <summary>An empty implementation of available actions.</summary>
public class DefaultWorkflowActions  : IWorkflowActions { }

/// <summary>
/// The result record that represents the outcome of a case workflow trigger.
/// </summary>
/// <param name="Success">Indicating success or failure of the given operation</param>
/// <param name="CollectedWorkflows">A lightweight information regarding the collected worklow instances if any</param>
/// <param name="Message">A message related to the current operation</param>
public record WorkflowInvocationResult(bool Success, List<CasesCollectedWorkflow> CollectedWorkflows, string? Message = null);

/// <summary>
/// Represents a workflow that was either found in the database, or instantiated on the fly for initial execution.
/// In case the workflow instance ID was found in the DB, it will not yet have been loaded (which usually isn't necessary).
/// Otherwise, if the workflow instance was instantiated on the fly for new workflow runs, the workflow instance is provided to give the caller a chance to persist it into the database.
/// The reason for treating existing workflow instances and new ones differently is to prevent new workflow instances from being persisted without the caller actually invoking them (with input).
/// This causes un-started (Idle) workflows with no bound input, causing them to fail when a new application instance starts.
/// </summary>
public record CasesCollectedWorkflow(string WorkflowInstanceId, string? ActivityId);

/// <inheritdoc/>
internal class DefaultCasesWorkflowManager : ICasesWorkflowManager
{
    /// <inheritdoc/>
    public Task<WorkflowInvocationResult> InvokeApprovalAsync(UserActor user, Guid caseId, ApprovalRequest request) {
        return Task.FromResult(new WorkflowInvocationResult(Success: false, [], "Not implemented"));
    }
    
    /// <inheritdoc/>
    public Task<WorkflowInvocationResult> InvokeEditAsync(UserActor user, Guid caseId, string? comment, EditCaseRequest request) {
        return Task.FromResult(new WorkflowInvocationResult(Success: false, [], "Not implemented"));
    }
    
    /// <inheritdoc/>
    public Task<WorkflowInvocationResult> InvokeAssignmentAsync(Guid caseId, UserActor user) {
        return Task.FromResult(new WorkflowInvocationResult(Success: false, [], "Not implemented"));
    }

    public Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(UserActor user, Guid caseId) {
        return Task.FromResult(new List<RejectReason>());
    }

    /// <inheritdoc/>
    public Task<WorkflowInvocationResult> TriggerActionAsync(UserActor user, Guid caseId, ActionRequest request) {
        return Task.FromResult(new WorkflowInvocationResult(Success: false, [], "Not implemented"));
    }

    /// <inheritdoc/>
    public Task<CaseActions> GetAvailableActionsAsync(UserActor user, Guid caseId, string? assignedToId, string[] bookmarks, string? lastApprovedById = null) {
        return Task.FromResult(new CaseActions());
    }

    public Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode, UserActor workflowActor) {
        return Task.FromResult(new WorkflowInvocationResult(Success: false, [], "Not implemented"));
    }

    public Task<IWorkflowActions> GetActionsByCaseId(Guid caseId) {
        return Task.FromResult<IWorkflowActions>(new DefaultWorkflowActions());
    }
}

