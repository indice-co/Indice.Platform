using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskResultStoreEntityFrameworkCore : IRiskResultStore
{
    private readonly RiskDbContext _dbContext;

    public RiskResultStoreEntityFrameworkCore(RiskDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task CreateAsync(DbAggregateRuleExecutionResult riskResult) {
        _dbContext.RiskResults.Add(riskResult);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddEventIdAsync(Guid resultId, Guid eventId) {
        var riskResult = await _dbContext.RiskResults.FindAsync(resultId) ?? throw new Exception("Risk Result not found.");
        riskResult.EventId = eventId;
        await _dbContext.SaveChangesAsync(); 
    }

    public async Task<ResultSet<DbAggregateRuleExecutionResult>> GetList(ListOptions<AdminRiskFilter> options) {
        var query = _dbContext.RiskResults.AsNoTracking().AsQueryable();
        query = ApplyFilters(query, options.Filter.Filter);
        return await query.ToResultSetAsync(options);
    }

    private IQueryable<DbAggregateRuleExecutionResult> ApplyFilters(IQueryable<DbAggregateRuleExecutionResult> query, FilterClause[] filter) {
        var dateFromClause = filter.FirstOrDefault(c => c.Member.ToLower() == "from");
        var dateToClause = filter.FirstOrDefault(c => c.Member.ToLower() == "to");

        if (DateTimeOffset.TryParse(dateFromClause.Value, out var dateFrom) && DateTimeOffset.TryParse(dateToClause.Value, out var dateTo)) {
            if (dateFrom == dateTo) {
                dateTo = dateTo.Date.AddDays(1).AddTicks(-1);
            }

            query = query.Where(c => c.CreatedAt >= dateFrom);
            query = query.Where(c => c.CreatedAt <= dateTo);
        }

        foreach (var clause in filter) {
            if (string.IsNullOrWhiteSpace(clause.Member)) {
                continue;
            }

            if (clause.Member.Equals(nameof(DbAggregateRuleExecutionResult.SubjectId), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => x.SubjectId.Equals(clause.Value));
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => !x.SubjectId.Equals(clause.Value));
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.SubjectId.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbAggregateRuleExecutionResult.Name), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.Equals(clause.Value));
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => !string.IsNullOrEmpty(x.Name) && !x.Name.Equals(clause.Value));
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => !string.IsNullOrEmpty(x.Name) && x.Name.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbAggregateRuleExecutionResult.Type), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => x.Type.Equals(clause.Value));
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => !x.Type.Equals(clause.Value));
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.Type.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbAggregateRuleExecutionResult.IpAddress), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => !string.IsNullOrEmpty(x.IpAddress) && x.IpAddress.Equals(clause.Value));
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => !string.IsNullOrEmpty(x.IpAddress) && !x.IpAddress.Equals(clause.Value));
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => !string.IsNullOrEmpty(x.IpAddress) && x.IpAddress.Contains(clause.Value));
                        break;
                }
            }
        }

        return query;
    }
}
