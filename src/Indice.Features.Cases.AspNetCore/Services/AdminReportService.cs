using System.Security.Claims;
using Indice.Features.Cases.Data;
using Indice.Features.Cases.Data.Models;
using Indice.Features.Cases.Exceptions;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.EntityFrameworkCore;
using static Indice.Features.Cases.CasesApiConstants;

namespace Indice.Features.Cases.Services
{
    internal class AdminReportService : IAdminReportService
    {
        private readonly CasesDbContext _dbContext;
        private readonly ICaseAuthorizationProvider _roleCaseTypeProvider;
        private readonly ICaseTypeService _caseTypeService;

        public AdminReportService(
            CasesDbContext dbContext,
            ICaseAuthorizationProvider roleCaseTypeProvider,
            ICaseTypeService caseTypeService
            ) {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _roleCaseTypeProvider = roleCaseTypeProvider ?? throw new ArgumentNullException(nameof(roleCaseTypeProvider));
            _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
        }

        private async Task<List<GroupByReportResult>> GetCasesGroupedByCaseType(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
            .AsNoTracking()
                .Where(x => (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.CaseType.Code)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

        private async Task<List<GroupByReportResult>> GetAgentCasesGroupedByCaseType(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => x.Channel == Channels.Agent &&
                                (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.CaseType.Code)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

        private async Task<List<GroupByReportResult>> GetCustomerCasesGroupedByCaseType(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => x.Channel == Channels.Customer &&
                                (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.CaseType.Code)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

        private async Task<List<GroupByReportResult>> GetCasesGroupedByStatus(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.Checkpoint.CheckpointType.Status)
                .Select(group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() })
                .ToListAsync();
            return query;
        }
        private async Task<List<GroupByReportResult>> GetAgentCasesGroupedByStatus(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => x.Channel == Channels.Agent &&
                                (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.Checkpoint.CheckpointType.Status)
                .Select(group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() })
                .ToListAsync();
            return query;
        }

        private async Task<List<GroupByReportResult>> GetCustomerCasesGroupedByStatus(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => x.Channel == Channels.Customer &&
                                (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.Checkpoint.CheckpointType.Status)
                .Select(group => new GroupByReportResult { Label = group.Key.ToString(), Count = group.Count() })
                .ToListAsync();
            return query;
        }

        private async Task<List<GroupByReportResult>> GetCasesGroupedByGroupId(GetCasesListFilter filter) {
            List<GroupByReportResult> query = await _dbContext.Cases
                .AsNoTracking()
                .Where(x => (filter.GroupIds == null || (filter.GroupIds != null && filter.GroupIds.Any() && filter.GroupIds.Contains(x.GroupId))) &&
                                (filter.CaseTypeCodes == null || (filter.CaseTypeCodes != null && filter.GroupIds.Any() && filter.CaseTypeCodes.Contains(x.CaseType.Code))) &&
                                (filter.CheckpointTypeIds == null || (filter.CheckpointTypeIds != null && filter.CheckpointTypeIds.Any() && filter.CheckpointTypeIds.Contains(x.Checkpoint.CheckpointType.Id.ToString()))))
                .GroupBy(x => x.GroupId)
                .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
                .ToListAsync();
            return query;
        }

        public async Task<List<GroupByReportResult>> GenerateReport(ClaimsPrincipal user, ReportTag reportTag) {
            var filter = new GetCasesListFilter();
            // TODO: not crazy about this one
            // if a CaseAuthorizationService down the line wants to 
            // not allow a user to see the list of case, it throws a ResourceUnauthorizedException
            // which we catch and return an empty resultset. 
            try {
                filter = await _roleCaseTypeProvider.Filter(user, filter);
            } catch (ResourceUnauthorizedException) {
                return new List<GroupByReportResult>();
            }
            var groupByReportResult = new List<GroupByReportResult>();
            var caseTypes = new ResultSet<CaseTypePartial>();
            switch (reportTag) {
                case ReportTag.GroupedByCasetype:
                    groupByReportResult = await GetCasesGroupedByCaseType(filter);
                    caseTypes = await _caseTypeService.Get(user, false);
                    // translate
                    groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title);
                    return groupByReportResult;
                case ReportTag.AgentGroupedByCasetype:
                    groupByReportResult = await GetAgentCasesGroupedByCaseType(filter);
                    caseTypes = await _caseTypeService.Get(user, false);
                    // translate
                    groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title);
                    return groupByReportResult;
                case ReportTag.CustomerGroupedByCasetype:
                    groupByReportResult = await GetCustomerCasesGroupedByCaseType(filter);
                    caseTypes = await _caseTypeService.Get(user, false);
                    // translate
                    groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title);
                    return groupByReportResult;
                case ReportTag.GroupedByStatus:
                    return await GetCasesGroupedByStatus(filter);
                case ReportTag.AgentGroupedByStatus:
                    return await GetAgentCasesGroupedByStatus(filter);
                case ReportTag.CustomerGroupedByStatus:
                    return await GetCustomerCasesGroupedByStatus(filter);
                case ReportTag.GroupedByGroupId:
                    return await GetCasesGroupedByGroupId(filter);
                default:
                    throw new ArgumentException(nameof(reportTag));
            }
        }

    }

}