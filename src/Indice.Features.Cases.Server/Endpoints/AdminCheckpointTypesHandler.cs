using System.Security.Claims;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminCheckpointTypesHandler
{
    public static async Task<Results<Ok<List<CheckpointType>>, NotFound>> GetDistinctCheckpointTypes(ICheckpointTypeService checkpointTypeService, ClaimsPrincipal user) =>
        TypedResults.Ok(await checkpointTypeService.GetDistinctCheckpointTypes(user));
}

