using System.Text.Json;
using Elsa;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks;
using Indice.Features.Cases.Workflows.Extensions;
using Indice.Features.Cases.Workflows.Models;
using Indice.Features.Cases.Workflows.Models.Decision;
using Indice.Features.Cases.Workflows.Services;
using Indice.Features.Cases.Workflows.Services.Abstractions;
using Indice.Features.Cases.Workflows.Specifications;
using Indice.Features.Cases.Workflows.Store;
using Indice.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Rule = RulesEngine.Models.Rule;

namespace Indice.Features.Cases.Workflows.Endpoints;

internal static class WorkflowManagerHandler
{
    public static async Task<Results<NoContent, ValidationProblem>> SetDecisionRules(
        string caseTypeCode,
        DecisionTable decisionTable,
        DecisionStore store,
        IWorkflowDefinitionStore workflowDefinitionStore
    ) {
        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(caseTypeCode);
        var workflowDefinition = await workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification);
        if (workflowDefinition == null) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: $"SetDecisionRules failed. There is no workflow definition with the tag: {caseTypeCode}.");
        }
        
        var definitions = GetDecisionDefinitionsInternal(workflowDefinition);
        if (definitions.Count == 0) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: $"SetDecisionRules failed. There are no decision definitions with the tag: {caseTypeCode}.");
        }
        
        var rulesList = decisionTable.Rules.Select(x => new Rule {
            RuleName = x.RuleName,
            SuccessEvent = x.SuccessEvent,
            ErrorMessage = x.ErrorMessage,
            Expression = x.Conditions.BuildExpression()
        }).ToArray();

        var validationResult = new Z3Validator().Validate(definitions.First(), rulesList); // todo: correctly handle multiple decisions in casetype
        if (!validationResult.Success) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: validationResult.Error);
        }
        
        await store.CreateRules(caseTypeCode, decisionTable, rulesList);

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<DecisionsResponse>, ValidationProblem>> GetDecisionDefinitions(
        string caseTypeCode,
        DecisionStore store,
        IWorkflowDefinitionStore workflowDefinitionStore
    ) {
        var workflowDefinitionTagSpecification = new WorkflowDefinitionTagCsvSpecification(caseTypeCode);
        var workflowDefinition = await workflowDefinitionStore.FindAsync(workflowDefinitionTagSpecification);
        if (workflowDefinition == null) {
            return TypedResults.ValidationProblem(errors: new Dictionary<string, string[]>(), detail: $"GetDecisionDefinitions failed. There is no workflow definition with the tag: {caseTypeCode}.");
        }
        
        var decisionDefinitions = GetDecisionDefinitionsInternal(workflowDefinition);
        var decisionTable = await store.GetDecisionTable(caseTypeCode, decisionDefinitions.FirstOrDefault()!.Name);
        return TypedResults.Ok(new DecisionsResponse {
            DecisionDefinitions = decisionDefinitions,
            DecisionTable = decisionTable
        });
    }

    public class DecisionsResponse
    {
        public IList<DecisionDefinition> DecisionDefinitions { get; set; }
        public DecisionTable? DecisionTable { get; set; }
    }
    
    private static IList<DecisionDefinition> GetDecisionDefinitionsInternal(WorkflowDefinition workflowDefinition) {
        IList<DecisionDefinition> definitions = new List<DecisionDefinition>();
        
        var decisionActivities = workflowDefinition.Activities.Where(a => a.Type == nameof(DecisionActivity));
        foreach (var activity in decisionActivities) {
            var decisionName = activity.GetProperty(nameof(DecisionActivity.DecisionName));
            var decisionVariables = activity.GetProperty(nameof(DecisionActivity.DecisionVariables));
            var variables = JsonSerializer.Deserialize<List<DecisionVariableDefinition>>(decisionVariables!, JsonSerializerOptionDefaults.GetDefaultSettings());
            
            definitions.Add(new DecisionDefinition {
                Name = decisionName!,
                Variables = variables!,
            });
        }

        return definitions;
    }
    
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
        ArgumentOutOfRangeException.ThrowIfEqual(request.ActionId, Guid.Empty); 
        
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
    
    public static async Task<Ok<AvailableActions>> GetActionsByCaseId(
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

        return TypedResults.Ok(new AvailableActions {
            AssignmentBookmarks = assignmentBookmarks,
            EditBookmarks = editBookmarks,
            ApprovalBookmarks = approvalBookmarks,
            CustomActions = await GetCustomActions(caseId, bookmarkFinder, workflowInstanceStore)
        });
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