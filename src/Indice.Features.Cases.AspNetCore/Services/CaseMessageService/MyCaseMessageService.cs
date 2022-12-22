using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services.CaseMessageService
{
    internal class MyCaseMessageService : BaseCaseMessageService, IMyCaseMessageService
    {
        private readonly CasesDbContext _dbContext;

        public MyCaseMessageService(
            CasesDbContext dbContext,
            ICaseEventService caseEventService,
            ISchemaValidator schemaValidator)
            : base(dbContext, caseEventService, schemaValidator) {
            _dbContext = dbContext;
        }

        public async Task<Guid?> Send(Guid caseId, ClaimsPrincipal user, Message message) {
            var @case = await GetMyCase(caseId, user);
            return await SendInternal(@case, message, user);
        }

        public async Task Send(Guid caseId, ClaimsPrincipal user, Exception exception, string message = null) {
            var @case = await GetMyCase(caseId, user);
            await SendInternal(@case, user, exception, message);
        }

        private async Task<DbCase> GetMyCase(Guid caseId, ClaimsPrincipal user) {
            if (caseId == Guid.Empty) {
                throw new ArgumentException(nameof(caseId));
            };
            var userId = user.FindSubjectId();
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
}