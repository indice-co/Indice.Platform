using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal class AdminQueriesHandler
{
    public static async Task<Ok<List<Query>>> GetQueries(IQueryService queryService, ClaimsPrincipal currentUser) {
        var queries = await queryService.GetQueries(currentUser);
        return TypedResults.Ok(queries);
    }

    public static async Task<NoContent> SaveQuery(SaveQueryRequest request, IQueryService queryService, ClaimsPrincipal currentUser) {
        await queryService.SaveQuery(currentUser, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteQuery(Guid queryId, IQueryService queryService, ClaimsPrincipal currentUser) {
        var success = await queryService.DeleteQuery(currentUser, queryId);
        if (!success) { 
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
