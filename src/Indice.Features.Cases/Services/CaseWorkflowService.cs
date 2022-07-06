using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class CaseWorkflowService : ICaseWorkflowService
    {
        private readonly CasesDbContext _dbContext;

        public CaseWorkflowService(CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<IEnumerable<Guid>> GetCaseIdsForUpdate(int topResults, string checkpointTypeNameSuffix, string? caseTypeCode) {
            // Get all the checkpoint type Ids for the given caseTypeCode or suffix.
            var checkpointTypeIds = await _dbContext.CheckpointTypes
                .AsNoTracking()
                .AsQueryable()
                .Where(p => p.CaseType.Code == (string.IsNullOrEmpty(caseTypeCode) ? p.CaseType.Code : caseTypeCode)) // filter by caseTypeCode
                .Where(p => p.Code.EndsWith(checkpointTypeNameSuffix)) // filter by checkpoint name suffix
                .Select(p => p.Id)
                .ToListAsync();

            // Get all the checkpoints that exist
            var cases = await _dbContext.Checkpoints
                .AsNoTracking()
                .Where(p => checkpointTypeIds.Contains(p.CheckpointTypeId))
                .OrderBy(p => p.Case.CreatedBy.When)
                .Take(topResults)
                .Select(p => p.CaseId)
                .ToListAsync();

            return cases;
        }
    }
}