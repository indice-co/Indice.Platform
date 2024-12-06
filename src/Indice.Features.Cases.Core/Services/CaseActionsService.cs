using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Indice.Features.Cases.Core.Services;

internal class CaseActionsService : ICaseActionsService
{
    private readonly CasesDbContext _casesDbContext;
    private readonly ICasesWorkflowManager _workflowManager;
    private readonly ILogger<CaseActionsService> _logger;

    public CaseActionsService(
        CasesDbContext casesDbContext,
        ICasesWorkflowManager workflowManager,
        ILogger<CaseActionsService> logger
        ) {
        _casesDbContext = casesDbContext ?? throw new ArgumentNullException(nameof(casesDbContext));
        _workflowManager = workflowManager ?? throw new ArgumentNullException(nameof(workflowManager));
        this._logger = logger;
    }

    public async Task<CaseActions?> GetUserActions(ClaimsPrincipal user, Guid caseId) {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentOutOfRangeException.ThrowIfEqual(caseId, default);
        var @case = await _casesDbContext.Cases.Where(x => x.Id == caseId).Select(x => new { x.Id, x.AssignedTo }).FirstOrDefaultAsync();
        if (@case == null) {
            _logger.LogError("Case n not found for caseId {caseId}", caseId);
            return new CaseActions();
        }

        var caseIsAssigned = @case.AssignedTo != null;
        var isAssignedToCurrentUser = @case.AssignedTo?.Id == user.FindSubjectId();
        var userRoles = user
            .FindAll(claim => claim.Type == BasicClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();
        if (userRoles.Length == 0 && !user.IsSystemClient()) {
            return new CaseActions();
        }
        var lastApprovedById = await _casesDbContext.CaseApprovals
                                                    .Where(p => p.CaseId == caseId && p.Committed)
                                                    .OrderByDescending(p => p.CreatedBy.When)
                                                    .Select(p => p.CreatedBy.Id)
                                                    .FirstOrDefaultAsync();
        return await _workflowManager.GetAvailableActionsAsync(user, caseId, @case.AssignedTo?.Id, userRoles, lastApprovedById);
    }
}