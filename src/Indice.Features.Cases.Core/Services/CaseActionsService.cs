using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;

namespace Indice.Features.Cases.Core.Services;

internal class CaseActionsService : ICaseActionsService
{
    //private readonly IBookmarkFinder _bookmarkFinder;
    private readonly CasesDbContext _casesDbContext;
    private readonly ICaseApprovalService _caseApprovalService;
    //private readonly IWorkflowInstanceStore _workflowInstanceStore;

    public CaseActionsService(
        //IBookmarkFinder bookmarkFinder,
        CasesDbContext casesDbContext,
        ICaseApprovalService caseApprovalService
        //IWorkflowInstanceStore workflowInstanceStore
        ) {
        //_bookmarkFinder = bookmarkFinder ?? throw new ArgumentNullException(nameof(bookmarkFinder));
        _casesDbContext = casesDbContext ?? throw new ArgumentNullException(nameof(casesDbContext));
        _caseApprovalService = caseApprovalService ?? throw new ArgumentNullException(nameof(caseApprovalService));
        //_workflowInstanceStore = workflowInstanceStore ?? throw new ArgumentNullException(nameof(workflowInstanceStore));
    }

    public ValueTask<CaseActions> GetUserActions(ClaimsPrincipal user, Guid caseId) {
        //TODO: Workflow integration
        throw new NotImplementedException();
    }
}