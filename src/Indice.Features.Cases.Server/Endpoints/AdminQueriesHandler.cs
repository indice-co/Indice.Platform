using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal class AdminQueriesHandler
{
    public static async Task<Results<Ok<List<Query>>, NotFound>> GetQueries(IQueryService queryService, ClaimsPrincipal currentUser) {
        var queries = await queryService.GetQueries(currentUser);
        if (queries == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(queries);
    }

    public static async Task<NoContent> SaveQuery(SaveQueryRequest request, IQueryService queryService, ClaimsPrincipal user) {
        await queryService.SaveQuery(user, request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteQuery(Guid queryId, IQueryService queryService, ClaimsPrincipal user) {
        await queryService.DeleteQuery(user, queryId);
        return TypedResults.NoContent();
    }
}
