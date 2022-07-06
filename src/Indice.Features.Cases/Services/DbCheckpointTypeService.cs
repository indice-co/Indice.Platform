using System;
using System.Threading.Tasks;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class DbCheckpointTypeService : ICheckpointTypeService
    {
        private readonly CasesDbContext _dbContext;
        private readonly ICaseTypeService _caseTypeService;

        public DbCheckpointTypeService(
            CasesDbContext dbContext,
            ICaseTypeService caseTypeService) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
        }
        public async Task Create(Guid caseTypeId, string name, string? description, CasePublicStatus publicStatus) {
            if (name == null) throw new ArgumentNullException(nameof(name));

            var caseType = await _caseTypeService.Get(caseTypeId);

            var checkpointType = new DbCheckpointType {
                CaseTypeId = caseTypeId,
                Description = description,
                PublicStatus = publicStatus
            };

            checkpointType.SetCode(caseType.Code, name);

            await _dbContext.AddAsync(checkpointType);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<DbCheckpointType> GetCheckpointType(Guid caseTypeId, CasePublicStatus publicStatus) {
            return await _dbContext
                .CheckpointTypes
                .SingleAsync(p => p.CaseTypeId == caseTypeId && p.PublicStatus == publicStatus);
        }
    }
}