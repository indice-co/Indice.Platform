using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class LookupHandler
{
    public static async Task<Results<Ok<ResultSet<LookupItem>>, NotFound>> GetLookup(
        string lookupName, 
        [AsParameters] ListOptions options,
        FilterClause[]? filterTerms, 
        ILookupServiceFactory lookupServiceFactory) {
        var lookupService = lookupServiceFactory.Create(lookupName);
        var lookupItems = await lookupService.Get(ListOptions.Create(options, new LookupFilter { FilterTerms = filterTerms?.Select(x => new FilterTerm { Key = x.Member, Value = x.Value }).ToList() }));
        if (lookupItems == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(lookupItems);
    }
}
