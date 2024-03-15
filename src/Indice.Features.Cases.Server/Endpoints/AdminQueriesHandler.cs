using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal class AdminQueriesHandler
{
    public static async Task<Results<Ok<List<Query>>, NotFound>> GetQueries(IQueryService queryService, ClaimsPrincipal User) {
        var queries = await queryService.GetQueries(User);
        if (queries == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(queries);
    }

    public static async Task<NoContent> SaveQuery(IQueryService queryService, SaveQueryRequest request, ClaimsPrincipal User) {
        await queryService.SaveQuery(User, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteQuery(IQueryService queryService, Guid queryId, ClaimsPrincipal User) {
        await queryService.DeleteQuery(User, queryId);
        return TypedResults.NoContent();
    }
}
