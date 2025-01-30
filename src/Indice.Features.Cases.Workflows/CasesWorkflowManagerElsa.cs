using System.Globalization;
using System.Security.Claims;
using Indice.Security;
using Elsa;
using Elsa.Persistence;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Interfaces;
using Elsa.Models;
using Elsa.Persistence.Specifications;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Specifications;

namespace Indice.Features.Cases.Workflows;

/// <inheritdoc/>
/// <inheritdoc/>
internal class CasesWorkflowManagerElsa(
    IAwaitApprovalInvoker approvalInvoker,
    IAwaitAssignmentInvoker awaitAssignmentInvoker,
    IAwaitEditInvoker awaitEditInvoker,
    IAwaitActionInvoker awaitActionInvoker,
    IWorkflowInstanceStore workflowInstanceStore,
    IBookmarkFinder bookmarkFinder,
    IWorkflowDefinitionStore workflowDefinitionStore,
    IStartsWorkflow startsWorkflow,
    IWorkflowBlueprintMaterializer workflowBlueprintMaterializer)
{
    private readonly IWorkflowDefinitionStore _workflowDefinitionStore = workflowDefinitionStore ?? throw new ArgumentNullException(nameof(workflowDefinitionStore));
    private readonly IStartsWorkflow _startsWorkflow = startsWorkflow ?? throw new ArgumentNullException(nameof(startsWorkflow));
    private readonly IWorkflowBlueprintMaterializer _workflowBlueprintMaterializer = workflowBlueprintMaterializer ?? throw new ArgumentNullException(nameof(workflowBlueprintMaterializer));
    private readonly IAwaitApprovalInvoker _approvalInvoker = approvalInvoker ?? throw new ArgumentNullException(nameof(approvalInvoker));
    private readonly IAwaitAssignmentInvoker _awaitAssignmentInvoker = awaitAssignmentInvoker ?? throw new ArgumentNullException(nameof(awaitAssignmentInvoker));
    private readonly IAwaitEditInvoker _awaitEditInvoker = awaitEditInvoker ?? throw new ArgumentNullException(nameof(awaitEditInvoker));
    private readonly IAwaitActionInvoker _awaitActionInvoker = awaitActionInvoker ?? throw new ArgumentNullException(nameof(awaitActionInvoker));
    private readonly IWorkflowInstanceStore _workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
    private readonly IBookmarkFinder _bookmarkFinder = bookmarkFinder ?? throw new ArgumentNullException(nameof(bookmarkFinder));
    public record WorkflowInvocationResult(bool Success, List<CasesCollectedWorkflow> CollectedWorkflows, string? Message = null);
    public record CasesCollectedWorkflow(string WorkflowInstanceId, string? ActivityId);

    // todo: these are all breaking
    
    /// <inheritdoc/>
    public async Task<WorkflowInvocationResult> InvokeApprovalAsync(ClaimsPrincipal user, Guid caseId, ApprovalRequest request) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        // var executedWorkflow = await _approvalInvoker.ExecuteWorkflowsAsync(caseId, request);
        // if (!executedWorkflow.Any()) {
        //     return new CasesWorkflowResult(Success: false,
        //                                    executedWorkflow.Select(x => new CasesCollectedWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList(),
        //                                    "You cannot approve or reject case at this point.");
        // }
        return new WorkflowInvocationResult(Success: true, []);
    }
    
    /// <inheritdoc/>
    public async Task<WorkflowInvocationResult> InvokeEditAsync(ClaimsPrincipal user, Guid caseId, EditCaseRequest request) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        // var executedWorkflow = await _awaitEditInvoker.ExecuteWorkflowsAsync(caseId, request);
        // if (!executedWorkflow.Any()) {
        //     return new CasesWorkflowResult(Success: false,
        //                                    executedWorkflow.Select(x => new CasesCollectedWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList(),
        //                                    "You cannot edit at this point.");
        // }
        return new WorkflowInvocationResult(Success: true, []);
    }

    /// <inheritdoc/>
    public async Task<WorkflowInvocationResult> AssignCaseAsync(ClaimsPrincipal user, Guid caseId, string outcome) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        // var input = new AwaitAssignmentInvokerInput {
        //     // Get the current user for self-assign
        //     // todo support admin assignments [in future user-story]
        //     User = AuditMeta.Create(user)
        // };
        // var executedWorkflow = await _awaitAssignmentInvoker.ExecuteWorkflowsAsync(caseId, input);
        // if (!executedWorkflow.Any()) {
        //     return new WorkflowInvocationResult(Success: false,
        //                                    executedWorkflow.Select(x => new CasesCollectedWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList(),
        //                                    "Case is already assigned.");
        // }
        return new WorkflowInvocationResult(Success: true, []);
    }

    /// <inheritdoc/>
    public async Task<WorkflowInvocationResult> TriggerActionAsync(ClaimsPrincipal user, Guid caseId, ActionRequest request) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        var executedWorkflow = await _awaitActionInvoker.ExecuteWorkflowsAsync(caseId, request);
        if (!executedWorkflow.Any()) {
            return new WorkflowInvocationResult(Success: false,
                                           executedWorkflow.Select(x => new CasesCollectedWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList(),
                                           $"You cannot perform this action at this point.");
        }
        return new WorkflowInvocationResult(Success: true, []);
    }

    public async Task<List<RejectReason>> GetApprovalRejectOptionsListAsync(ClaimsPrincipal user, Guid caseId) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        var instance = await _workflowInstanceStore.FindByCorrelationIdAsync(caseId.ToString());
        if (instance == null) {
            return [];
        }

        // todo: move caseSharedResourceService out of here
        var reasonsWorkflowVariable = instance.Variables.Get<IEnumerable<string>>(CasesWorkflowConstants.WorkflowVariables.RejectReasons) ?? Enumerable.Empty<string>();
        var reasons = reasonsWorkflowVariable.Select(item => new RejectReason {
            Key = item,
            // Value = _caseSharedResourceService.GetLocalizedHtmlString(item, CultureInfo.CurrentCulture.Name).Value
        });
        return reasons.ToList();
    }

    // todo: remove roles
    public async Task<object> GetActionsByCaseId(
        ClaimsPrincipal user,
        Guid caseId,
        string[] bookmarks
    ) {
        var assignmentBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitAssignmentActivity),
            bookmarks: bookmarks.Select(role => new AwaitAssignmentBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitAssignmentBookmark).ToList();
        var editBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitEditActivity),
            bookmarks: bookmarks.Select(role => new AwaitEditBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitEditBookmark).ToList();
        var approvalBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitApprovalActivity),
            bookmarks: bookmarks.Select(role => new AwaitApprovalBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitApprovalBookmark).ToList();
        var customCaseActions = await GetWorkflowCustomCaseActions(caseId, bookmarks);
        return new AvailableActions {
            AssignmentBookmarks = assignmentBookmarks,
            EditBookmarks = editBookmarks,
            ApprovalBookmarks = approvalBookmarks,
            CustomCaseActions = customCaseActions
        };
    }

    // todo: remove this
    /// <inheritdoc/>
    [Obsolete("This method is obsolete. Use GetActionsByCaseId instead.")]
    public async Task<CaseActions> GetAvailableActionsAsync(
        ClaimsPrincipal user,
        Guid caseId,
        string? assignedToId,
        string[] bookmarks,
        string? lastApprovedById = null) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);

        var caseIsAssigned = !string.IsNullOrWhiteSpace(assignedToId);
        var isAssignedToCurrentUser = caseIsAssigned && assignedToId == user.FindSubjectId();
        // Retrieve bookmarks for each blocking activity
        var assignmentBookmarks = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitAssignmentActivity),
            bookmarks: bookmarks.Select(role => new AwaitAssignmentBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        );
        var editBookmarks = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitEditActivity),
            bookmarks: bookmarks.Select(role => new AwaitEditBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        );
        var approvalBookmarks = (await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitApprovalActivity),
            bookmarks: bookmarks.Select(role => new AwaitApprovalBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        )).ToList();
        var userCanApprove = approvalBookmarks.Any();
        var blockPreviousApprover = approvalBookmarks.Any(p => ((AwaitApprovalBookmark)p.Bookmark).BlockPreviousApprover);
        if (blockPreviousApprover) {
            // Check if 4-eyes principle is enabled for this workflow instance
            // First get actor of the the latest checkpoint that has not been completed
            // Then check if the actor is the current user
            userCanApprove &= lastApprovedById != user.FindSubjectId();
        }
        if (caseIsAssigned) {
            // Allow approvals only when the user has the case assigned
            userCanApprove &= isAssignedToCurrentUser;
        }
        
        var customCaseActions = await GetCustomCaseActions(caseId, bookmarks);
        return user.IsAdmin() || user.IsSystemClient()
            ? new CaseActions {
                HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                HasApproval = approvalBookmarks.Any(),
                HasUnassignment = caseIsAssigned,
                HasEdit = editBookmarks.Any(),
                CustomActions = customCaseActions
            }
            : new CaseActions {
                HasApproval = userCanApprove,
                HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                HasEdit = editBookmarks.Any() && isAssignedToCurrentUser,
                CustomActions = customCaseActions
            };
    }

    public async Task<WorkflowInvocationResult> StartWorkflowAsync(Guid caseId, string caseTypeCode, AuditMeta auditMeta) {
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseTypeCode);

        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(caseTypeCode);
        var workflowDefinition = await _workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification);
        if (workflowDefinition == null) {
            return new (Success: true, [], "Nothing todo. No worflow definition found");
        }

        var blueprint = await _workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition);
        var instance = await _startsWorkflow.StartWorkflowAsync(
            blueprint,
            null,
            new WorkflowInput(caseId),
            caseId.ToString());

        if (instance.WorkflowInstance?.Faults is { Count: > 0 }) {
            return new(Success: false, [], $"Workflow failed to start. {instance.WorkflowInstance?.Faults.FirstOrDefault()?.Message}");
        }
        return new(Success: true, []);
    }


    /// <summary>Get the custom action blocking activities of type <see cref="AwaitActionActivity"/>.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="userRoles">The user roles.</param>
    /// <returns></returns>
    private async Task<List<CustomCaseAction>> GetCustomCaseActions(Guid caseId, IEnumerable<string> userRoles) {
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles = userRoles.Concat([string.Empty]);
        // Get workflow instance and get the activity data from the context
        var instance = await _workflowInstanceStore.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(caseId.ToString()));
        if (instance == null) {
            return [];
        }
        // Find all the current blocking activities with type "AwaitActionActivity"
        var activities = instance.BlockingActivities
            .Where(p => p.ActivityType == nameof(AwaitActionActivity))
            .Select(p => p.ActivityId)
            .ToList();
        if (activities.Count == 0) {
            return [];
        }
        
        var actionIds = instance.ActivityData
            .Where(p => activities.Contains(p.Key))
            // .Select(p => TransformActivityData(p.Value!))
            .Select(p => (string)p.Value[nameof(AwaitActionActivity.ActionId)]!);
        
        // Get a list of bookmarks with the action id and the role.
        var bookmarks = from actionId in actionIds
                        from userRole in userRoles
                        select new AwaitActionBookmark(caseId.ToString(), userRole, actionId);
        var actions = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitActionActivity),
            bookmarks: bookmarks,
            correlationId: caseId.ToString()
        );
        var activityIds = actions.Select(p => p.ActivityId).ToList();
        if (activityIds.Count == 0) {
            return [];
        }
        return instance!.ActivityData
            .Where(p => activityIds.Contains(p.Key))
            .Select(p => TransformActivityData(p.Value!)).ToList();
    }

    private static CustomCaseAction TransformActivityData(IDictionary<string, object> activityData) {
        return new CustomCaseAction {
            Id = (string)activityData[nameof(AwaitActionActivity.ActionId)],
            Name = activityData.TryGetValue(nameof(AwaitActionActivity.ActionName), out var name) ? name as string : null,
            Label = activityData.TryGetValue(nameof(AwaitActionActivity.ActionLabel), out var label) ? label as string : null,
            Class = activityData.TryGetValue(nameof(AwaitActionActivity.ActionClass), out var @class) ? @class as string : null,
            RedirectToList = activityData.TryGetValue(nameof(AwaitActionActivity.RedirectToList), out var redirectToList) ? redirectToList as bool? : false,
            SuccessMessage = activityData.TryGetValue(nameof(AwaitActionActivity.SuccessMessage), out var successMessage) ? successMessage as SuccessMessage : null,
            DefaultValue = activityData.TryGetValue(nameof(AwaitActionActivity.ActionInputDefaultValue), out var defaultValue) ? defaultValue as string : null,
            Description = activityData.TryGetValue(nameof(AwaitActionActivity.ActionDescription), out var description) ? description as string : null,
            HasInput = activityData.TryGetValue(nameof(AwaitActionActivity.ShowInput), out var hasInput) ? hasInput as bool? : false
        };
    }
    
    /// <summary>Get the custom action blocking activities of type <see cref="AwaitActionActivity"/>.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="userRoles">The user roles.</param>
    /// <returns></returns>
    private async Task<List<WorkflowCustomCaseAction>> GetWorkflowCustomCaseActions(Guid caseId, IEnumerable<string> userRoles) {
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles = userRoles.Concat([string.Empty]);
        // Get workflow instance and get the activity data from the context
        var instance = await _workflowInstanceStore.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(caseId.ToString()));
        if (instance == null) {
            return [];
        }
        // Find all the current blocking activities with type "AwaitActionActivity"
        var activities = instance.BlockingActivities
            .Where(p => p.ActivityType == nameof(AwaitActionActivity))
            .Select(p => p.ActivityId)
            .ToList();
        if (activities.Count == 0) {
            return [];
        }
        
        var actionIds = instance.ActivityData
            .Where(p => activities.Contains(p.Key))
            .Select(p => (string)p.Value[nameof(AwaitActionActivity.ActionId)]!);
        
        // Get a list of bookmarks with the action id and the role.
        var bookmarks = from actionId in actionIds
                        from userRole in userRoles
                        select new AwaitActionBookmark(caseId.ToString(), userRole, actionId);
        var actions = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitActionActivity),
            bookmarks: bookmarks,
            correlationId: caseId.ToString()
        );
        var activityIds = actions.Select(p => p.ActivityId).ToList();
        if (activityIds.Count == 0) {
            return [];
        }
        return instance!.ActivityData
            .Where(p => activityIds.Contains(p.Key))
            .Select(p => TransformWorkflowActivityData(p.Value!)).ToList();
    }
    
    private static WorkflowCustomCaseAction TransformWorkflowActivityData(IDictionary<string, object> activityData) {
        return new WorkflowCustomCaseAction() {
            Id = (string)activityData[nameof(AwaitActionActivity.ActionId)],
            Name = activityData.TryGetValue(nameof(AwaitActionActivity.ActionName), out var name) ? name as string : null,
            Label = activityData.TryGetValue(nameof(AwaitActionActivity.ActionLabel), out var label) ? label as string : null,
            Class = activityData.TryGetValue(nameof(AwaitActionActivity.ActionClass), out var @class) ? @class as string : null,
            RedirectToList = activityData.TryGetValue(nameof(AwaitActionActivity.RedirectToList), out var redirectToList) ? redirectToList as bool? : false,
            SuccessMessage = activityData.TryGetValue(nameof(AwaitActionActivity.SuccessMessage), out var successMessage) ? successMessage as WorkflowSuccessMessage : null,
            DefaultValue = activityData.TryGetValue(nameof(AwaitActionActivity.ActionInputDefaultValue), out var defaultValue) ? defaultValue as string : null,
            Description = activityData.TryGetValue(nameof(AwaitActionActivity.ActionDescription), out var description) ? description as string : null,
            HasInput = activityData.TryGetValue(nameof(AwaitActionActivity.ShowInput), out var hasInput) ? hasInput as bool? : false
        };
    }
}
