using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminCheckpointTypesHandler
{
    public static async Task<Results<Ok<List<CheckpointType>>, NotFound>> GetDistinctCheckpointTypes(ICheckpointTypeService checkpointTypeService, ClaimsPrincipal User) {
        var distinctCheckpointTypes = await checkpointTypeService.GetDistinctCheckpointTypes(User);
        if (distinctCheckpointTypes == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(distinctCheckpointTypes);

    }
}

