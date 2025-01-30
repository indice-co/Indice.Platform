using System.Security.Claims;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Integration;
using Indice.Features.Cases.Workflows.Interfaces;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Microsoft.AspNetCore.Routing;

internal static class WorkflowManagerHandler
{
    public static async Task<Results<NoContent, ProblemHttpResult>> StartWorkflow(
        Guid caseId,
        string caseTypeCode,
        CasesUser casesUser,
        IAwaitApprovalInvoker approvalInvoker,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowBlueprintMaterializer workflowBlueprintMaterializer,
        IStartsWorkflow startsWorkflow,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseTypeCode);

        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(caseTypeCode);
        var workflowDefinition = await workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification, cancellationToken);
        if (workflowDefinition == null) {
            return TypedResults.NoContent();
        }
        
        workflowDefinition.Variables.Set("initialActor", casesUser);
        workflowDefinition.Variables.Set("actor", casesUser);
        var blueprint = await workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
        var instance = await startsWorkflow.StartWorkflowAsync(
            blueprint,
            null,
            new WorkflowInput(caseId),
            caseId.ToString(), cancellationToken: cancellationToken);
        
        

        if (instance.WorkflowInstance?.Faults is { Count: > 0 }) {
            return TypedResults.Problem(detail: $"Workflow failed to start. {instance.WorkflowInstance?.Faults.FirstOrDefault()?.Message}");
        }
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeApproval(
        WorkflowSubmitApprovalRequest request,
        IAwaitApprovalInvoker approvalInvoker,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        var executedWorkflow = await approvalInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot approve or reject case at this point.");
            // return new CasesWorkflowResult(Success: false,
            //     executedWorkflow.Select(x => new CasesCollectedWorkflow(x.WorkflowInstanceId, x.ActivityId)).ToList(),
            //     "You cannot approve or reject case at this point.");
        }
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeEdit(
        WorkflowEditCaseRequest request,
        IAwaitEditInvoker editInvoker,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        
        var executedWorkflow = await editInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot edit at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeAssignment(
        WorkflowAssignCaseRequest request,
        IAwaitAssignmentInvoker assignmentInvoker,
        CancellationToken cancellationToken = default) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        
        var executedWorkflow = await assignmentInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot assign at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<WorkflowCasesActions> GetAvailableActions(
        ClaimsPrincipal user,
        Guid caseId,
        string? assignedToId,
        string[] bookmarks,
        IBookmarkFinder bookmarkFinder,
        IWorkflowInstanceStore workflowInstanceStore,
        string subjectId,
        bool isAdmin,
        bool isSystemClient,
        string? lastApprovedById = null) {
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, Guid.Empty);

        var caseIsAssigned = !string.IsNullOrWhiteSpace(assignedToId);
        var isAssignedToCurrentUser = caseIsAssigned && assignedToId == subjectId;
        // Retrieve bookmarks for each blocking activity
        var assignmentBookmarks = await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitAssignmentActivity),
            bookmarks: bookmarks.Select(role => new AwaitAssignmentBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        );
        var editBookmarks = await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitEditActivity),
            bookmarks: bookmarks.Select(role => new AwaitEditBookmark(caseId.ToString(), role)),
            correlationId: caseId.ToString()
        );
        var approvalBookmarks = (await bookmarkFinder.FindBookmarksAsync(
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
            userCanApprove &= lastApprovedById != subjectId;
        }
        if (caseIsAssigned) {
            // Allow approvals only when the user has the case assigned
            userCanApprove &= isAssignedToCurrentUser;
        }
        
        var customCaseActions = await GetCustomCaseActions(caseId, bookmarks, workflowInstanceStore, bookmarkFinder);
        return isAdmin || isSystemClient
            ? new WorkflowCasesActions {
                HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                HasApproval = approvalBookmarks.Any(),
                HasUnassignment = caseIsAssigned,
                HasEdit = editBookmarks.Any(),
                CustomActions = customCaseActions
            }
            : new WorkflowCasesActions {
                HasApproval = userCanApprove,
                HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                HasEdit = editBookmarks.Any() && isAssignedToCurrentUser,
                CustomActions = customCaseActions
            };
    }
    
    /// <summary>Get the custom action blocking activities of type <see cref="AwaitActionActivity"/>.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="userRoles">The user roles.</param>
    /// <returns></returns>
    private static async Task<List<WorkflowCustomCaseAction>> GetCustomCaseActions(
        Guid caseId,
        IEnumerable<string> userRoles,
        IWorkflowInstanceStore workflowInstanceStore,
        IBookmarkFinder bookmarkFinder) {
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles = userRoles.Concat([string.Empty]);
        // Get workflow instance and get the activity data from the context
        var instance = await workflowInstanceStore.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(caseId.ToString()));
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
        var actions = await bookmarkFinder.FindBookmarksAsync(
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

    // todo: create elsa store service to handle bookmark and workflow instance data retrieval
    public static async Task<AvailableActions> GetActionsByCaseId(
        Guid caseId,
        string[] bookmarks,
        IBookmarkFinder bookmarkFinder,
        IWorkflowInstanceStore workflowInstanceStore) {
        var assignmentBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitAssignmentActivity),
            bookmarks: [new AwaitAssignmentBookmark(caseId.ToString())],
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitAssignmentBookmark).ToList();
        var editBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitEditActivity),
            bookmarks: [new AwaitEditBookmark(caseId.ToString())],
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitEditBookmark).ToList();
        var approvalBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitApprovalActivity),
            bookmarks: [new AwaitApprovalBookmark(caseId.ToString())],
            correlationId: caseId.ToString()
        )).Select(x => x.Bookmark as AwaitApprovalBookmark).ToList();
        var customCaseActions = await GetWorkflowCustomCaseActions(caseId, bookmarkFinder, workflowInstanceStore);
        
        return new AvailableActions {
            AssignmentBookmarks = assignmentBookmarks,
            EditBookmarks = editBookmarks,
            ApprovalBookmarks = approvalBookmarks,
            CustomCaseActions = customCaseActions
        };
    }
    
    // todo: remove from here
    private static async Task<List<WorkflowCustomCaseAction>> GetWorkflowCustomCaseActions(
        Guid caseId,
        // IEnumerable<string> userRoles,
        IBookmarkFinder bookmarkFinder,
        IWorkflowInstanceStore workflowInstanceStore) {
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        // userRoles = userRoles.Concat([string.Empty]);
        // Get workflow instance and get the activity data from the context
        var instance = await workflowInstanceStore.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(caseId.ToString()));
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
                        // from userRole in userRoles
                        select new AwaitActionBookmark(caseId.ToString(), actionId);
        var actions = await bookmarkFinder.FindBookmarksAsync(
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
    
    // todo: remove from here
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