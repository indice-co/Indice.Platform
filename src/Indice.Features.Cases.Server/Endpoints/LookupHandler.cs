using Indice.Features.Cases.Factories;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class LookupHandler
{
    public static async Task<Results<Ok<ResultSet<LookupItem>>, NotFound>> GetLookup(string lookupName, [AsParameters] ListOptions options, FilterClause[]? filter , ILookupServiceFactory lookupServiceFactory) {
        var lookupService = lookupServiceFactory.Create(lookupName);
        var lookupItems = await lookupService.Get(ListOptions.Create(options, filter?.ToList() ?? []));
        if (lookupItems == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(lookupItems);
    }
}
