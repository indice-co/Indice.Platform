using Elsa;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Services.Abstractions;
using Indice.Features.Cases.Workflows.Specifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Workflows.Endpoints;

internal static class WorkflowManagerHandler
{
    public static async Task<Results<NoContent, ValidationProblem>> StartWorkflow(
        Guid caseId,
        string caseTypeCode,
        Actor actor,
        IWorkflowDefinitionStore workflowDefinitionStore,
        IWorkflowBlueprintMaterializer workflowBlueprintMaterializer,
        IStartsWorkflow startsWorkflow,
        CancellationToken cancellationToken = default
    ) {
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, Guid.Empty);
        ArgumentException.ThrowIfNullOrWhiteSpace(caseTypeCode);

        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(caseTypeCode);
        var workflowDefinition = await workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification, cancellationToken);
        if (workflowDefinition == null) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: $"Workflow failed to start. There is no workflow definition with the tag: {caseTypeCode}.");
        }
        
        workflowDefinition.Variables.Set(CasesWorkflowConstants.WorkflowVariables.Actor.Initiator, actor);
        workflowDefinition.Variables.Set(CasesWorkflowConstants.WorkflowVariables.Actor.Current, actor);
        var blueprint = await workflowBlueprintMaterializer.CreateWorkflowBlueprintAsync(workflowDefinition, cancellationToken);
        var runWorkflowResult = await startsWorkflow.StartWorkflowAsync(
            blueprint,
            null,
            new WorkflowInput(caseId),
            caseId.ToString(), cancellationToken: cancellationToken);

        if (runWorkflowResult.WorkflowInstance?.Faults is { Count: > 0 }) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: $"Workflow failed to start. {runWorkflowResult.WorkflowInstance?.Faults.FirstOrDefault()?.Message}");
        }
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeApproval(
        InvokeApprovalRequest request,
        IAwaitApprovalInvoker approvalInvoker,
        CancellationToken cancellationToken = default
    ) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request.Actor);
        
        var executedWorkflow = await approvalInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot approve or reject case at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeEdit(
        InvokeEditRequest request,
        IAwaitEditInvoker editInvoker,
        CancellationToken cancellationToken = default
    ) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request.Actor);
        
        var executedWorkflow = await editInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot edit at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeAction(
        InvokeActionRequest request,
        IAwaitActionInvoker actionInvoker,
        CancellationToken cancellationToken = default
    ) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request.Actor);
        ArgumentNullException.ThrowIfNull(request.ActionId);
        
        var executedWorkflow = await actionInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot edit at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<Results<NoContent, ProblemHttpResult>> InvokeAssignment(
        InvokeAssignmentRequest request,
        IAwaitAssignmentInvoker assignmentInvoker,
        CancellationToken cancellationToken = default
    ) {
        ArgumentOutOfRangeException.ThrowIfEqual(request.CaseId, Guid.Empty);
        ArgumentNullException.ThrowIfNull(request.Actor);
        
        var executedWorkflow = await assignmentInvoker.ExecuteWorkflowsAsync(request.CaseId, request, cancellationToken);
        if (!executedWorkflow.Any()) {
            return TypedResults.Problem(detail: "You cannot assign at this point.");
        }
        
        return TypedResults.NoContent();
    }
    
    public static async Task<AvailableActions> GetActionsByCaseId(
        Guid caseId,
        IBookmarkFinder bookmarkFinder,
        IWorkflowInstanceStore workflowInstanceStore
    ) {
        var assignmentBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            nameof(AwaitAssignmentActivity),
            [new AwaitAssignmentBookmark(caseId.ToString())],
            caseId.ToString()
        )).Select(x => x.Bookmark as AwaitAssignmentBookmark).ToList();
        var editBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            nameof(AwaitEditActivity),
            [new AwaitEditBookmark(caseId.ToString())],
            caseId.ToString()
        )).Select(x => x.Bookmark as AwaitEditBookmark).ToList();
        var approvalBookmarks = (await bookmarkFinder.FindBookmarksAsync(
            nameof(AwaitApprovalActivity),
            [new AwaitApprovalBookmark(caseId.ToString())],
            caseId.ToString()
        )).Select(x => x.Bookmark as AwaitApprovalBookmark).ToList();

        return new AvailableActions {
            AssignmentBookmarks = assignmentBookmarks,
            EditBookmarks = editBookmarks,
            ApprovalBookmarks = approvalBookmarks,
            CustomActions = await GetCustomActions(caseId, bookmarkFinder, workflowInstanceStore)
        };
    }

    public static async Task<IEnumerable<string>> GetRejectReasonsByCaseId(Guid caseId, IWorkflowInstanceStore workflowInstanceStore) {
        var instance = await workflowInstanceStore.FindByCorrelationIdAsync(caseId.ToString());
        if (instance == null) {
            return [];
        }
        
        return instance.Variables.Get<IEnumerable<string>>(CasesWorkflowConstants.WorkflowVariables.RejectReasons) ?? [];
    }
    
    private static async Task<List<CustomAction>?> GetCustomActions(
        Guid caseId,
        IBookmarkFinder bookmarkFinder,
        IWorkflowInstanceStore workflowInstanceStore
    ) {
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
            .Select(p => TransformWorkflowActivityData(p.Value!))
            .Select(p => p.Id);
        
        // Get a list of bookmarks with the action id and the role.
        var bookmarks = from actionId in actionIds
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
        return instance.ActivityData
            .Where(p => activityIds.Contains(p.Key))
            .Select(p => TransformWorkflowActivityData(p.Value!)).ToList();
    }
    
    private static CustomAction TransformWorkflowActivityData(IDictionary<string, object> activityData) {
        return new CustomAction {
            Id = (string)activityData[nameof(AwaitActionActivity.ActionId)],
            AllowedRole = activityData.TryGetValue(nameof(AwaitActionActivity.AllowedRole), out var role) ? role as string : null,
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

}