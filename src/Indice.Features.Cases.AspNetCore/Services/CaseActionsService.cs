using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Elsa.Services;
using IdentityModel;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Workflows.Activities;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitApproval;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitAssignment;
using Indice.Features.Cases.Workflows.Bookmarks.AwaitEdit;
using Indice.Security;

namespace Indice.Features.Cases.Services
{
    internal class CaseActionsService : ICaseActionsService
    {
        private readonly IBookmarkFinder _bookmarkFinder;
        private readonly CasesDbContext _casesDbContext;
        private readonly ICaseApprovalService _caseApprovalService;

        public CaseActionsService(
            IBookmarkFinder bookmarkFinder,
            CasesDbContext casesDbContext,
            ICaseApprovalService caseApprovalService) {
            _bookmarkFinder = bookmarkFinder ?? throw new ArgumentNullException(nameof(bookmarkFinder));
            _casesDbContext = casesDbContext ?? throw new ArgumentNullException(nameof(casesDbContext));
            _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        }

        public async ValueTask<CaseActions> GeUserActions(ClaimsPrincipal user, Guid caseId) {
            if (caseId == default) throw new ArgumentException("CaseId not present.", nameof(caseId));

            var @case = await _casesDbContext.Cases.FindAsync(caseId);
            if (@case == null) throw new ArgumentNullException(nameof(@case), "Case not valid.");

            var caseIsAssigned = @case.AssignedTo != null;
            var isAssignedToCurrentUser = @case.AssignedTo?.Id == user.FindSubjectId();

            var userRoles = user
                .FindAll(claim => claim.Type == JwtClaimTypes.Role)
                .Select(claim => claim.Value)
                .ToList();

            if (!userRoles.Any()) {
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

            return user.IsAdmin()
                ? new CaseActions {
                    HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                    HasApproval = approvalBookmarks.Any(),
                    HasUnassignment = caseIsAssigned,
                    HasEdit = editBookmarks.Any()
                }
                : new CaseActions {
                    HasApproval = userCanApprove,
                    HasAssignment = assignmentBookmarks.Any() && !caseIsAssigned,
                    HasEdit = editBookmarks.Any() && isAssignedToCurrentUser
                };
        }
    }
}