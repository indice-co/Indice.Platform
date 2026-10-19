using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Models.Requests;
using Indice.Types;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminCheckpointTypesHandlers
{
    public static async Task<Results<Ok<List<CheckpointType>>, NotFound>> GetDistinctCheckpointTypes(ICheckpointTypeService checkpointTypeService, 
        ClaimsPrincipal User,
        IOptions<CasesOptions> casesOptions) {
        var distinctCheckpointTypes = await checkpointTypeService.GetDistinctCheckpointTypes(User.UserToActor(casesOptions.Value));
        if (distinctCheckpointTypes == null) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(distinctCheckpointTypes);

    }

    public static async Task<Ok<ResultSet<CheckpointType>>> GetCheckpointTypesList(ICheckpointTypeService checkpointTypeService,
        ClaimsPrincipal User,
        IOptions<CasesOptions> casesOptions,
        [AsParameters] ListOptions options,
        [AsParameters] GetCheckpointTypesListFilter filter) =>
        TypedResults.Ok(await checkpointTypeService.GetList(User.UserToActor(casesOptions.Value), ListOptions.Create(options, filter)));

    public static async Task<Results<Ok<CheckpointType>, NotFound>> GetCheckpointTypeById(ICheckpointTypeService checkpointTypeService, Guid checkpointTypeId) {
        if (await checkpointTypeService.GetById(checkpointTypeId) is not { } checkpointType) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(checkpointType);
    }

    public static async Task<CreatedAtRoute<CheckpointType>> CreateCheckpointType(ICheckpointTypeService checkpointTypeService, CheckpointTypeRequest request) {
        var checkpointType = await checkpointTypeService.Create(request);
        return TypedResults.CreatedAtRoute(checkpointType, nameof(GetCheckpointTypeById), new { checkpointTypeId = checkpointType.Id });
    }

    public static async Task<Results<Ok<CheckpointType>, NotFound>> UpdateCheckpointType(ICheckpointTypeService checkpointTypeService, Guid checkpointTypeId, CheckpointTypeRequest request) {
        if (await checkpointTypeService.Update(checkpointTypeId, request) is not { } checkpointType) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(checkpointType);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCheckpointType(ICheckpointTypeService checkpointTypeService, Guid checkpointTypeId) {
        if (!await checkpointTypeService.Delete(checkpointTypeId)) {
            return TypedResults.NotFound();
        }
        return TypedResults.NoContent();
    }
}
