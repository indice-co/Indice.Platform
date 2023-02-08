using Indice.Features.Cases.Data;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Services
{
    internal class AdminReportService : IAdminReportService
    {
        private readonly CasesDbContext _dbContext;

        public AdminReportService(
            CasesDbContext dbContext) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<List<GroupByReportResult>> GetCasesGroupedByStatus() {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .GroupBy(x => x.Checkpoint.CheckpointType.Status)
                .Select(group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() })
                .ToListAsync();
            return query;
        }

        public async Task<List<GroupByReportResult>> GetCasesGroupedByCaseType() {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .GroupBy(x => x.CaseType.Code)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

        public async Task<List<GroupByReportResult>> GetCasesGroupedByGroupId() {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .GroupBy(x => x.GroupId)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

    }
}