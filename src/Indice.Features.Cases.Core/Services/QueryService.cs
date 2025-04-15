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

    public async Task<List<Query>> GetQueries(UserActor user) {
        return await _dbContext.Queries
            .AsQueryable()
            .Where(q => q.UserId == user.Id)
            .Select(q => new Query {
                Id = q.Id,
                FriendlyName = q.FriendlyName,
                Parameters = q.Parameters
            })
            .ToListAsync();
    }

    public async Task SaveQuery(UserActor user, SaveQueryRequest request) {
        var dbQuery = new DbQuery {
            UserId = user.Id!,
            FriendlyName = request.FriendlyName,
            Parameters = request.Parameters!,
        };
        // save query
        await _dbContext.AddAsync(dbQuery);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> DeleteQuery(UserActor user, Guid queryId) {
        var dbQuery = await _dbContext.Queries.Where(x => x.Id == queryId && x.UserId != user.Id).FirstOrDefaultAsync();
        // someone tries to delete someone else's query!
        if (dbQuery is null) {
            return false;
        }
        _dbContext.Queries.Remove(dbQuery);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}