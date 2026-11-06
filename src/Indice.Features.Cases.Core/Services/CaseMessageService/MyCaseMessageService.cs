using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Events;
using Indice.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Indice.Features.Cases.Core.Services.CaseMessageService;

/// <inheritdoc />
internal class MyCaseMessageService : BaseCaseMessageService, IMyCaseMessageService
{

    public MyCaseMessageService(
        CasesDbContext dbContext,
        IPlatformEventService platformEventService,
        ISchemaValidator schemaValidator,
        [FromKeyedServices(CasesCoreConstants.FileServiceKey)] IFileService? fileService = null)
        : base(dbContext, platformEventService, schemaValidator, fileService) {
    }

    /// <inheritdoc />
    public async Task<Guid?> Send(Guid caseId, UserActor user, Message message, AuditMeta createdBy) {
        var @case = await GetMyCase(caseId, user);
        return await SendInternal(@case, message, createdBy);
    }

    private async Task<DbCase> GetMyCase(Guid caseId, UserActor user) {
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