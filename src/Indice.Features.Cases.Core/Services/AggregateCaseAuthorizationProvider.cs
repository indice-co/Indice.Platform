using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class AggregateCaseAuthorizationProvider : ICaseAuthorizationProvider
{
    private readonly IEnumerable<ICaseAuthorizationService> _caseAuthorizationServices;
    private readonly CasesDbContext _dbContext;
    public AggregateCaseAuthorizationProvider(CasesDbContext dbContext, IEnumerable<ICaseAuthorizationService> listOfServices) {
        _caseAuthorizationServices = listOfServices ?? throw new ArgumentNullException(nameof(listOfServices));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<IQueryable<CasePartial>> GetCaseMembership(IQueryable<CasePartial> cases, WorkflowActor user) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            cases = await authorizationService.GetCaseMembership(cases, user);
        }
        return cases;
    }

    public async Task<bool> IsMember(WorkflowActor user, Case @case) {
        foreach (var authorizationService in _caseAuthorizationServices) {
            if (!await authorizationService.IsMember(user, @case)) {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> IsMember(WorkflowActor user, Guid caseId) {
        var dbcase = await _dbContext.Cases
                .AsNoTracking()
                .Include(x => x.Checkpoint)
                .FirstOrDefaultAsync(x => x.Id == caseId);

        if (dbcase == null) { return false; }

        // Create a case details just for the authorization, with the min required properties
        var caseDetails = new Case {
            Id = dbcase.Id,
            CaseType = new CaseTypePartial {
                Id = dbcase.CaseTypeId
            },
            GroupId = dbcase.GroupId,
            CheckpointType = new CheckpointType {
                Id = dbcase.Checkpoint.CheckpointTypeId,
            },
            CreatedById = dbcase.CreatedBy.Id
        };
        return await IsMember(user, caseDetails);
    }
}