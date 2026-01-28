using Indice.Features.Risk.Core.Abstractions;
using Indice.Features.Risk.Core.Data;
using Indice.Features.Risk.Core.Data.Models;
using Indice.Features.Risk.Core.Models.Requests;
using Indice.Types;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Risk.Core.Stores;

internal class RiskEventStoreEntityFrameworkCore : IRiskEventStore
{
    private readonly RiskDbContext _dbContext;

    public RiskEventStoreEntityFrameworkCore(RiskDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<DbRiskEvent> CreateAsync(DbRiskEvent @event) {
        _dbContext.RiskEvents.Add(@event);
        await _dbContext.SaveChangesAsync();
        return @event;
    }

    public async Task<IEnumerable<DbRiskEvent>> GetList(
        string subjectId,
        string[]? names,
        DateTime? startDate,
        DateTime? endDate,
        List<FilterClause>? filters
    ) {
        var query = _dbContext
            .RiskEvents
            .Where(x => x.SubjectId == subjectId);

        if (names?.Any() == true) {
            query = query.Where(x => names.Contains(x.Name));
        }
        if (startDate.HasValue) {
            query = query.Where(x => x.CreatedAt >= startDate);
        }
        if (endDate.HasValue) {
            query = query.Where(x => x.CreatedAt <= endDate);
        }
        if (filters?.Any() == true) {
            query = query.Where(filters);
        }

        return await query.ToListAsync();
    }

    public async Task<ResultSet<DbRiskEvent>> GetList(ListOptions<AdminRiskEventFilterRequest> options) {
        var query = _dbContext.RiskEvents.AsNoTracking().AsQueryable();
        query = ApplyFilter(query, options.Filter);
        return await query.ToResultSetAsync(options);
    }

    public async Task<IEnumerable<DbRiskEvent>> GetRiskEventsBySessionId(string sessionId) {
        var query = _dbContext.RiskEvents.AsNoTracking().Where(x => x.SessionId == sessionId);
        return await query.ToListAsync();
    }

    private IQueryable<DbRiskEvent> ApplyFilter(IQueryable<DbRiskEvent> query, AdminRiskEventFilterRequest filters) {
        foreach (var clause in filters.Filter) {
            if (string.IsNullOrWhiteSpace(clause.Member) || string.IsNullOrWhiteSpace(clause.Value)) {
                continue;
            }

            if (clause.Member.ToLower() == "from" && DateTimeOffset.TryParse(clause.Value, out var dateFrom)) {
                query = query.Where(c => c.CreatedAt >= dateFrom);
            }

            if (clause.Member.ToLower() == "from" && DateTimeOffset.TryParse(clause.Value, out var dateTo)) {
                query = query.Where(c => c.CreatedAt <= dateTo);
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.Id), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        if (Guid.TryParse(clause.Value, out var clauseIdValue)) {
                            // Create a local copy to capture the current value.
                            var currentId = clauseIdValue;
                            query = query.Where(x => currentId == x.Id);
                        }
                        break;
                    case FilterOperator.Neq:
                        if (Guid.TryParse(clause.Value, out var notEqualsClauseIdValue)) {
                            // Create a local copy to capture the current value.
                            var currentId = notEqualsClauseIdValue;
                            query = query.Where(x => currentId != x.Id);
                        }
                        break;
                    case FilterOperator.Contains:
                        // Contains is not applicable for GUIDs skip.
                        // We should consider throwing an exception here to inform the caller.
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.SubjectId), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.SubjectId);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.SubjectId);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.SubjectId.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.Name), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.Name);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.Name);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.Name!.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.Type), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.Type);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.Type);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.Type.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.IpAddress), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.IpAddress);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.IpAddress);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.IpAddress!.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.SessionId), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.SessionId);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.SessionId);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.SessionId!.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.CountryIsoCode), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    // Country ISO codes are stored in uppercase.
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value.ToUpper() == x.CountryIsoCode);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value.ToUpper() != x.CountryIsoCode);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.CountryIsoCode!.Contains(clause.Value.ToUpper()));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.Location), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.Location);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.Location);
                        break;
                    case FilterOperator.Contains:
                        query = query.Where(x => x.Location!.Contains(clause.Value));
                        break;
                }
            }

            if (clause.Member.Equals(nameof(DbRiskEvent.SourceTransId), StringComparison.OrdinalIgnoreCase)) {
                switch (clause.Operator) {
                    case FilterOperator.Eq:
                        query = query.Where(x => clause.Value == x.SourceTransId);
                        break;
                    case FilterOperator.Neq:
                        query = query.Where(x => clause.Value != x.SourceTransId);
                        break;
                    case FilterOperator.Contains:
                        // Contains is translated by EF Core to SQL LIKE which is case-insensitive for SQL Server by default. This is not true for example PostgreSQL.
                        // If you are using PostgreSQL you might need to adjust this to use ILIKE or similar.
                        query = query.Where(x => x.SourceTransId!.Contains(clause.Value));
                        break;
                }
            }
        }
        return query;
    }
}
