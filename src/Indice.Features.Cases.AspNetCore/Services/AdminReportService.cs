using System.Linq.Expressions;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using static Indice.Features.Cases.CasesApiConstants;

namespace Indice.Features.Cases.Services
{
    internal class AdminReportService : IAdminReportService
    {
        private readonly CasesDbContext _dbContext;
        private readonly IDistributedCache _distributedCache;
        private readonly string _caseReportCacheKey = $"{nameof(AdminReportService)}.caseReport";

        public AdminReportService(
            CasesDbContext dbContext,
            IDistributedCache distributedCache
            ) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        }

        public async Task<List<GroupByReportResult>> GenerateReport(ReportTag reportTag) {
            switch (reportTag) {
                case ReportTag.GroupedByCasetype:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.GroupedByCasetype}",
                        getSourceAsync: async () => await RunQuery(
                            x => true,
                            x => x.CaseType.Code,
                            group => new GroupByReportResult { Label = group.Key, Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.AgentGroupedByCasetype:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.AgentGroupedByCasetype}",
                        getSourceAsync: async () => await RunQuery(
                            x => x.Channel == Channels.Agent,
                            x => x.CaseType.Code,
                            group => new GroupByReportResult { Label = group.Key, Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.CustomerGroupedByCasetype:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.CustomerGroupedByCasetype}",
                        getSourceAsync: async () => await RunQuery(
                            x => x.Channel == Channels.Customer,
                            x => x.CaseType.Code,
                            group => new GroupByReportResult { Label = group.Key, Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.GroupedByStatus:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.GroupedByStatus}",
                        getSourceAsync: async () => await RunQuery(
                            x => true,
                            x => x.Checkpoint.CheckpointType.Status,
                            group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.AgentGroupedByStatus:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.AgentGroupedByStatus}",
                        getSourceAsync: async () => await RunQuery(
                            x => x.Channel == Channels.Agent,
                            x => x.Checkpoint.CheckpointType.Status,
                            group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.CustomerGroupedByStatus:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.CustomerGroupedByStatus}",
                        getSourceAsync: async () => await RunQuery(
                            x => x.Channel == Channels.Customer,
                            x => x.Checkpoint.CheckpointType.Status,
                            group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                case ReportTag.GroupedByGroupId:
                    return await _distributedCache.TryGetAndSetAsync(
                        cacheKey: $"{_caseReportCacheKey}.{ReportTag.GroupedByGroupId}",
                        getSourceAsync: async () => await RunQuery(
                            x => true,
                            x => x.GroupId,
                            group => new GroupByReportResult { Label = group.Key, Count = group.Count() }),
                        options: new DistributedCacheEntryOptions {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                        });
                default:
                    throw new ArgumentException(nameof(reportTag));
            }
        }

        private async Task<List<GroupByReportResult>> RunQuery<Tkey>(
        Expression<Func<DbCase, bool>> whereClause,
        Expression<Func<DbCase, Tkey>> groupByClause,
        Expression<Func<IGrouping<Tkey, DbCase>, GroupByReportResult>> selectClause) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(whereClause)
                .GroupBy(groupByClause)
                .Select(selectClause)
                .ToListAsync();
            return query;
        }
    }

}