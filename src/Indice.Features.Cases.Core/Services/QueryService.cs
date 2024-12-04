using System.Security.Claims;
using Indice.Features.Cases.Core.Data;
using Indice.Features.Cases.Core.Data.Models;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Security;
using Microsoft.EntityFrameworkCore;

namespace Indice.Features.Cases.Core.Services;

internal class QueryService : IQueryService
{
    private readonly CasesDbContext _dbContext;

    public QueryService(CasesDbContext dbContext) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<List<Query>> GetQueries(ClaimsPrincipal user) {
        return await _dbContext.Queries
            .AsQueryable()
            .Where(q => q.UserId == user.FindSubjectId())
            .Select(q => new Query {
                Id = q.Id,
                FriendlyName = q.FriendlyName,
                Parameters = q.Parameters
            })
            .ToListAsync();
    }

    public async Task SaveQuery(ClaimsPrincipal user, SaveQueryRequest request) {
        var dbQuery = new DbQuery {
            UserId = user.FindSubjectId()!,
            FriendlyName = request.FriendlyName,
            Parameters = request.Parameters!,
        };
        // save query
        await _dbContext.AddAsync(dbQuery);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteQuery(ClaimsPrincipal user, Guid queryId) {
        var dbQuery = await Get(queryId);
        // someone tries to delete someone else's query!
        if (dbQuery.UserId != user.FindSubjectId()) {
            throw new Exception("Query is invalid.");
        }
        _dbContext.Queries.Remove(dbQuery);
        await _dbContext.SaveChangesAsync();
    }

    private async Task<DbQuery> Get(Guid queryId) {
        if (queryId == Guid.Empty) {
            throw new ArgumentNullException(nameof(queryId));
        }
        var dbQuery = await _dbContext.Queries.FindAsync(queryId);
        return dbQuery ?? throw new Exception("Query is invalid.");
    }
}