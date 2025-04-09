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

    public MyCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator)
        : base(dbContext, platformEventService, schemaValidator) {
    }

    /// <inheritdoc />
    public async Task<Guid?> Send(Guid caseId, WorkflowActor user, Message message, AuditMeta createdBy) {
        var @case = await GetMyCase(caseId, user);
        return await SendInternal(@case, message, createdBy);
    }

    private async Task<DbCase> GetMyCase(Guid caseId, WorkflowActor user) {
        if (caseId == Guid.Empty) {
            throw new ArgumentException(nameof(caseId));
        }
        var userId = user.Id;
        if (string.IsNullOrEmpty(userId)) {
            throw new ArgumentException(nameof(userId));
        }
        var @case = await DbContext.Cases.AsQueryable().FirstOrDefaultAsync(c => c.Id == caseId && c.CreatedBy.Id == userId);
        if (@case == null) {
            throw new ArgumentNullException(nameof(caseId), "Case not found");
        }
        return @case;
    }
}