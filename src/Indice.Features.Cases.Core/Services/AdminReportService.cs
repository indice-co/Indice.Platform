using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Exceptions;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class AdminReportService : IAdminReportService
{
    private readonly CasesDbContext _dbContext;
    private readonly ICaseAuthorizationProvider _memberAuthorizationProvider;
    private readonly ICaseTypeService _caseTypeService;

    public AdminReportService(
        CasesDbContext dbContext,
        ICaseAuthorizationProvider memberAuthorizationProvider,
        ICaseTypeService caseTypeService
        ) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _memberAuthorizationProvider = memberAuthorizationProvider ?? throw new ArgumentNullException(nameof(memberAuthorizationProvider));
        _caseTypeService = caseTypeService ?? throw new ArgumentNullException(nameof(caseTypeService));
    }

    public async Task<List<GroupByReportResult>> GenerateReport(ClaimsPrincipal user, ReportTag reportTag) {
        var query = _dbContext.Cases
            .AsNoTracking()
            .Where(c => !c.Draft) // filter out draft cases
            .Select(@case => new CasePartial {
                Id = @case.Id,
                CreatedByWhen = @case.CreatedBy.When,
                CaseType = new CaseTypePartial {
                    Id = @case.CaseType.Id,
                    Code = @case.CaseType.Code!,
                    Title = @case.CaseType.Title,
                },
                Channel = @case.Channel,
                GroupId = @case.GroupId,
                CheckpointType = new CheckpointType {
                    Id = @case.Checkpoint.CheckpointType.Id,
                    Code = @case.Checkpoint.CheckpointType.Code,
                    Status = @case.Checkpoint.CheckpointType.Status
                }
            });

        // TODO: not crazy about this one
        // if a CaseAuthorizationService down the line wants to 
        // not allow a user to see the list of case, it throws a ResourceUnauthorizedException
        // which we catch and return an empty resultset.
        try {
            query = await _memberAuthorizationProvider.GetCaseMembership(query, user);
        } catch (ResourceUnauthorizedException) {
            return new List<GroupByReportResult>();
        }
        List<GroupByReportResult> groupByReportResult;
        var caseTypes = new ResultSet<CaseTypePartial>();
        switch (reportTag) {
            case ReportTag.GroupedByCasetype:
                groupByReportResult = await GetCasesGroupedByCaseType(query);
                caseTypes = await _caseTypeService.Get(user, false);
                // translate
                groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title!);
                return groupByReportResult;
            case ReportTag.AgentGroupedByCasetype:
                groupByReportResult = await GetAgentCasesGroupedByCaseType(query);
                caseTypes = await _caseTypeService.Get(user, false);
                // translate
                groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title!);
                return groupByReportResult;
            case ReportTag.CustomerGroupedByCasetype:
                groupByReportResult = await GetCustomerCasesGroupedByCaseType(query);
                caseTypes = await _caseTypeService.Get(user, false);
                // translate
                groupByReportResult.ForEach(x => x.Label = caseTypes.Items.First(c => c.Code == x.Label).Title!);
                return groupByReportResult;
            case ReportTag.GroupedByStatus:
                return await GetCasesGroupedByStatus(query);
            case ReportTag.AgentGroupedByStatus:
                return await GetAgentCasesGroupedByStatus(query);
            case ReportTag.CustomerGroupedByStatus:
                return await GetCustomerCasesGroupedByStatus(query);
            case ReportTag.GroupedByGroupId:
                return await GetCasesGroupedByGroupId(query);
            default:
                throw new ArgumentException(nameof(reportTag));
        }
    }


    private async Task<List<GroupByReportResult>> GetCasesGroupedByCaseType(IQueryable<CasePartial> cases) {
        return await cases
            .GroupBy(x => x.CaseType.Code)
            .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
            .ToListAsync();
    }

    private async Task<List<GroupByReportResult>> GetAgentCasesGroupedByCaseType(IQueryable<CasePartial> cases) {
        return await cases
            .Where(x => x.Channel == CasesCoreConstants.Channels.Agent)
            .GroupBy(x => x.CaseType.Code)
            .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
            .ToListAsync();
    }

    private async Task<List<GroupByReportResult>> GetCustomerCasesGroupedByCaseType(IQueryable<CasePartial> cases) {
        return await cases
            .Where(x => x.Channel == CasesCoreConstants.Channels.Customer)
            .GroupBy(x => x.CaseType.Code)
            .Select(group => new GroupByReportResult { Label = group.Key, Count = group.Count() })
            .ToListAsync();
    }

    private async Task<List<GroupByReportResult>> GetCasesGroupedByStatus(IQueryable<CasePartial> cases) {
        return await cases
            .GroupBy(x => x.CheckpointType.Status)
            .Select(group => new GroupByReportResult { Label = group.Key.ToString()!, Count = group.Count() })
            .ToListAsync();
    }
    private async Task<List<GroupByReportResult>> GetAgentCasesGroupedByStatus(IQueryable<CasePartial> cases) {
        return await cases
            .Where(x => x.Channel == CasesCoreConstants.Channels.Agent)
            .GroupBy(x => x.CheckpointType.Status)
            .Select(group => new GroupByReportResult { Label = group.Key.ToString()!, Count = group.Count() })
            .ToListAsync();
    }

    private async Task<List<GroupByReportResult>> GetCustomerCasesGroupedByStatus(IQueryable<CasePartial> cases) {
        return await cases
            .Where(x => x.Channel == CasesCoreConstants.Channels.Customer)
            .GroupBy(x => x.CheckpointType.Status)
            .Select(group => new GroupByReportResult { Label = group.Key.ToString()!, Count = group.Count() })
            .ToListAsync();
    }

    private async Task<List<GroupByReportResult>> GetCasesGroupedByGroupId(IQueryable<CasePartial> cases) {
        return await cases
            .GroupBy(x => x.GroupId)
            .Select(group => new GroupByReportResult { Label = group.Key ?? string.Empty, Count = group.Count() })
            .ToListAsync();
    }

}