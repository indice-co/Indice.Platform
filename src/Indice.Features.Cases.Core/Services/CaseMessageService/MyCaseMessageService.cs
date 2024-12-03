using System.Security.Claims;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Events;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

/// <inheritdoc />
internal class MyCaseMessageService : BaseCaseMessageService, IMyCaseMessageService
{
    private readonly CasesDbContext _dbContext;

    public MyCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator)
        : base(dbContext, platformEventService, schemaValidator) {
        _dbContext = dbContext; //TODO: this should be used and exposed by the base class
    }
    /// <inheritdoc />
    public async Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message) {
        var @case = await GetMyCase(caseId, user);
        return await SendInternal(@case, message, user);
    }

    /// <inheritdoc />
    public async Task Send(Guid caseId, ClaimsPrincipal user, Exception exception, string? message = null) {
        var @case = await GetMyCase(caseId, user);
        await SendInternal(@case, user, exception, message);
    }

    private async Task<DbCase> GetMyCase(Guid caseId, ClaimsPrincipal user) {
        if (caseId == Guid.Empty) {
            throw new ArgumentException(nameof(caseId));
        };
        var userId = user.FindSubjectIdOrClientId();
        if (string.IsNullOrEmpty(userId)) {
            throw new ArgumentException(nameof(userId));
        }
        var @case = await _dbContext.Cases.AsQueryable().FirstOrDefaultAsync(c => c.Id == caseId && c.CreatedBy.Id == userId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(@case));
        }
        return @case;
    }
}