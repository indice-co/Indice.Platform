using System.Security.Claims;
using Indice.Events;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

internal class AdminCaseMessageService : BaseCaseMessageService, IAdminCaseMessageService
{
    private readonly ICaseAuthorizationProvider _caseAuthorization;

    public AdminCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator,
        ICaseAuthorizationProvider caseAuthorization)
        : base(dbContext, platformEventService, schemaValidator) {
        _caseAuthorization = caseAuthorization ?? throw new ArgumentNullException(nameof(caseAuthorization));
    }

    public async Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message) {
        var @case = await GetAdminCase(caseId, user);
        return await SendInternal(@case, message, user);
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

        if (await DbContext.Cases
                .Include(x => x.Checkpoint)
                .FirstOrDefaultAsync(x => x.Id == caseId) is not { } @case) {
            throw new ArgumentNullException(nameof(@case));
        }

        if (@case.CheckpointId is null && @case.Draft) {
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
            CheckpointType = new CheckpointType {
                Id= @case.Checkpoint.CheckpointTypeId,
            },
            CreatedById = @case.CreatedBy.Id
        };

        if (!await _caseAuthorization.IsMember(user, caseDetails)) {
            throw new ResourceUnauthorizedException();
        }
        return @case;
    }
}