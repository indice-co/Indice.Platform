using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Open.Linq.AsyncExtensions;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminCheckpointTypesHandler
{
    public static async Task<Results<Ok<List<CheckpointType>>, NotFound>> GetDistinctCheckpointTypes(ICheckpointTypeService checkpointTypeService, ClaimsPrincipal User) {
        var distinctCheckpointTypes = await checkpointTypeService.GetDistinctCheckpointTypes(User).ToList();
        if (distinctCheckpointTypes == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(distinctCheckpointTypes);
    
    }
}

