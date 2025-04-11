using Indice.Events;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

internal class AdminCaseMessageService : BaseCaseMessageService, IAdminCaseMessageService
{

    public AdminCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator)
        : base(dbContext, platformEventService, schemaValidator) {
    }

    // todo: remove actor
    public async Task<Guid?> Send(Guid caseId, WorkflowActor user, Message message, AuditMeta createdBy) {
        var @case = await GetAdminCase(caseId);
        return await SendInternal(@case, message, createdBy);
    }

    private async Task<DbCase> GetAdminCase(Guid caseId) {
        if (caseId == Guid.Empty) {
            throw new ArgumentException(nameof(caseId));
        }

        if (await DbContext.Cases
                .Include(x => x.Checkpoint)
                .FirstOrDefaultAsync(x => x.Id == caseId) is not { } @case) {
            throw new ArgumentNullException(nameof(caseId), "Case not found");
        }

        return @case;
    }
}