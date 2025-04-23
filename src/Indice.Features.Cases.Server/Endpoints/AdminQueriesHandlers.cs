using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
internal class AdminQueriesHandlers
{
    public static async Task<Ok<List<Query>>> GetQueries(IQueryService queryService, ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        var queries = await queryService.GetQueries(currentUser.UserToActor(casesOptions.Value));
        return TypedResults.Ok(queries);
    }

    public static async Task<NoContent> SaveQuery(SaveQueryRequest request, IQueryService queryService, ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        await queryService.SaveQuery(currentUser.UserToActor(casesOptions.Value), request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<NoContent, NotFound>> DeleteQuery(Guid queryId, IQueryService queryService, ClaimsPrincipal currentUser,
        IOptions<CasesOptions> casesOptions) {
        var success = await queryService.DeleteQuery(currentUser.UserToActor(casesOptions.Value), queryId);
        if (!success) { 
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
