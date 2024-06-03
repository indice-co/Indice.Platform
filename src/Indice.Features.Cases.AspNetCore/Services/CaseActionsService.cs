using System.Security.Claims;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Persistence.Specifications;
using Elsa.Services;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAction;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit;
using Indice.Security;

namespace Indice.Features.Cases.Services;

internal class CaseActionsService : ICaseActionsService
{
    private readonly IBookmarkFinder _bookmarkFinder;
    private readonly CasesDbContext _casesDbContext;
    private readonly ICaseApprovalService _caseApprovalService;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public CaseActionsService(
        IBookmarkFinder bookmarkFinder,
        CasesDbContext casesDbContext,
        ICaseApprovalService caseApprovalService,
        IWorkflowInstanceStore workflowInstanceStore) {
        _bookmarkFinder = bookmarkFinder ?? throw new ArgumentNullException(nameof(bookmarkFinder));
        _casesDbContext = casesDbContext ?? throw new ArgumentNullException(nameof(casesDbContext));
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        _workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
    }

    public async ValueTask<CaseActions> GetUserActions(ClaimsPrincipal user, Guid caseId) {
        if (caseId == default) throw new ArgumentException("CaseId not present.", nameof(caseId));

        var @case = await _casesDbContext.Cases.FindAsync(caseId);
        if (@case == null) throw new ArgumentNullException(nameof(@case), "Case not valid.");

        var caseIsAssigned = @case.AssignedTo != null;
        var isAssignedToCurrentUser = @case.AssignedTo?.Id == user.FindSubjectId();

        var userRoles = user
            .FindAll(claim => claim.Type == BasicClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToList();

        if (!userRoles.Any() && !user.IsSystemClient()) {
            return new CaseActions();
        }

        // Retrieve bookmarks for each blocking activity
        var assignmentBookmarks = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitAssignmentActivity),
            bookmarks: userRoles.Select(userRole => new AwaitAssignmentBookmark(caseId.ToString(), userRole)),
            correlationId: caseId.ToString()
        );
        var editBookmarks = await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitEditActivity),
            bookmarks: userRoles.Select(userRole => new AwaitEditBookmark(caseId.ToString(), userRole)),
            correlationId: caseId.ToString()
        );
        var approvalBookmarks = (await _bookmarkFinder.FindBookmarksAsync(
            activityType: nameof(AwaitApprovalActivity),
            bookmarks: userRoles.Select(userRole => new AwaitApprovalBookmark(caseId.ToString(), userRole)),
            correlationId: caseId.ToString()
        )).ToList();

        var userCanApprove = approvalBookmarks.Any();
        var blockPreviousApprover = approvalBookmarks.Any(p => ((AwaitApprovalBookmark)p.Bookmark).BlockPreviousApprover);
        if (blockPreviousApprover) {
            // Check if 4-eyes principle is enabled for this workflow instance
            // First get actor of the the latest checkpoint that has not been completed
            var lastApproval = await _caseApprovalService.GetLastApproval(caseId);

            // Then check if the actor is the current user
            userCanApprove &= lastApproval?.CreatedBy.Id != user.FindSubjectId();
        }

        if (caseIsAssigned) {
            // Allow approvals only when the user has the case assigned
            userCanApprove &= isAssignedToCurrentUser;
        }

        var customCaseActions = await GetCustomCaseActions(caseId, userRoles);

        return user.IsAdmin()
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

    /// <summary>Get the custom action blocking activities of type <see cref="AwaitActionActivity"/>.</summary>
    /// <param name="caseId">The Id of the case.</param>
    /// <param name="userRoles">The user roles.</param>
    /// <returns></returns>
    private async Task<IEnumerable<CustomCaseAction>> GetCustomCaseActions(Guid caseId, IEnumerable<string> userRoles) {
        // Always provide an empty string as a role in order to handle "null" allowed Roles of activity input.
        userRoles = userRoles.Concat(new[] { string.Empty });

        // Get workflow instance and get the activity data from the context
        var instance = await _workflowInstanceStore.FindAsync(new CorrelationIdSpecification<WorkflowInstance>(caseId.ToString()));
        if (instance == null) {
            return Enumerable.Empty<CustomCaseAction>();
        }

        // Find all the blocking activities with type "AwaitActionActivity"
        var activities = instance.BlockingActivities
            .Where(p => p.ActivityType == nameof(AwaitActionActivity))
            .Select(p => p.ActivityId)
            .ToList();

        if (!activities.Any()) {
            return Enumerable.Empty<CustomCaseAction>();
        }

        var actionIds = instance.ActivityData
            .Where(p => activities.Contains(p.Key))
            .Select(p => TransformActivityData(p.Value!))
            .Select(p => p.Id);

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
        if (!activityIds.Any()) {
            return Enumerable.Empty<CustomCaseAction>();
        }

        return instance!.ActivityData
            .Where(p => activityIds.Contains(p.Key))
            .Select(p => TransformActivityData(p.Value!));
    }

    private static CustomCaseAction TransformActivityData(IDictionary<string, object> activityData) {
        return new CustomCaseAction {
            Id = activityData.ContainsKey(nameof(AwaitActionActivity.ActionId)) ? activityData[nameof(AwaitActionActivity.ActionId)] as string : null,
            Name = activityData.ContainsKey(nameof(AwaitActionActivity.ActionName)) ? activityData[nameof(AwaitActionActivity.ActionName)] as string : null,
            Label = activityData.ContainsKey(nameof(AwaitActionActivity.ActionLabel)) ? activityData[nameof(AwaitActionActivity.ActionLabel)] as string : null,
            Class = activityData.ContainsKey(nameof(AwaitActionActivity.ActionClass)) ? activityData[nameof(AwaitActionActivity.ActionClass)] as string : null,
            RedirectToList = activityData.ContainsKey(nameof(AwaitActionActivity.RedirectToList)) ? activityData[nameof(AwaitActionActivity.RedirectToList)] as bool? : false,
            SuccessMessage = activityData.ContainsKey(nameof(AwaitActionActivity.SuccessMessage)) ? activityData[nameof(AwaitActionActivity.SuccessMessage)] as SuccessMessage : null,
            DefaultValue = activityData.ContainsKey(nameof(AwaitActionActivity.ActionInputDefaultValue)) ? activityData[nameof(AwaitActionActivity.ActionInputDefaultValue)] as string : null,
            Description = activityData.ContainsKey(nameof(AwaitActionActivity.ActionDescription)) ? activityData[nameof(AwaitActionActivity.ActionDescription)] as string : null,
            HasInput = activityData.ContainsKey(nameof(AwaitActionActivity.ShowInput)) ? activityData[nameof(AwaitActionActivity.ShowInput)] as bool? : false
        };
    }
}