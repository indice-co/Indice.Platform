using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Extensions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services.CaseMessageService;

internal class AdminCaseMessageService : BaseCaseMessageService, IAdminCaseMessageService
{
    private readonly CasesDbContext _dbContext;
    private readonly ICaseAuthorizationService _caseAuthorizationService;

    public AdminCaseMessageService(
        CasesDbContext dbContext,
        ICaseEventService caseEventService,
        ISchemaValidator schemaValidator,
        ICaseAuthorizationService caseAuthorizationService)
        : base(dbContext, caseEventService, schemaValidator) {
        _dbContext = dbContext;
        _caseAuthorizationService = caseAuthorizationService ?? throw new ArgumentNullException(nameof(caseAuthorizationService));
    }

    public async Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message) {
        var @case = await GetAdminCase(caseId, user);
        return await SendInternal(@case, message, user);
    }

    public async Task Send(Guid caseId, ClaimsPrincipal user, Exception exception, string message = null) {
        var @case = await GetAdminCase(caseId, user);
        await SendInternal(@case, user, exception, message);
    }

    private async Task<DbCase> GetAdminCase(Guid caseId, ClaimsPrincipal user) {
        if (caseId == Guid.Empty) {
            throw new ArgumentException(nameof(caseId));
        };
        if (user == null) {
            throw new ArgumentNullException(nameof(user));
        }
        var userId = user.FindSubjectIdOrClientId();
        if (string.IsNullOrEmpty(userId)) {
            throw new ArgumentException(nameof(userId));
        }
        var @case = await _dbContext.Cases.FindAsync(caseId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(@case));
        }

        var latestCheckpoint = await _dbContext.Checkpoints
            .AsQueryable()
            .OrderByDescending(c => c.CreatedBy.When)
            .FirstOrDefaultAsync(c => c.CaseId == caseId);

        if (latestCheckpoint == null && @case.Draft) {
            // This is the case when a new draft is created from admin spa
            return @case;
        }

        // Create a case details just for the authorization, with the min required properties
        var caseDetails = new Case {
            Id = @case.Id,
            CaseType = new CaseTypePartial {
                Id = @case.CaseTypeId
            },
            GroupId = @case.GroupId,
            CheckpointTypeId = latestCheckpoint!.Id,
            CreatedById = @case.CreatedBy.Id
        };

        if (!await _caseAuthorizationService.IsValid(user, caseDetails)) {
            throw new ResourceUnauthorizedException();
        }
        return @case;
    }
}