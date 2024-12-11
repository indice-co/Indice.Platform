using System.Security.Claims;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class AdminCaseTypesHandler
{

    public static async Task<Results<Ok<ResultSet<CaseTypePartial>>, NotFound>> GetAdminCaseTypes(ICaseTypeService caseTypeService, ClaimsPrincipal User, bool canCreate = false) {
        var caseTypes = await caseTypeService.Get(User, canCreate);
        if (caseTypes == null) {
               return TypedResults.NotFound();
        }
        return TypedResults.Ok(caseTypes);
    }

    public static async Task<Results<Ok<CaseType>, NotFound>> GetCaseTypeById(ICaseTypeService caseTypeService, Guid caseTypeId) {
        if (await caseTypeService.GetCaseTypeDetailsById(caseTypeId) is not { } caseTypeDetails) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(caseTypeDetails);
    }

    public static async Task<Results<NoContent, NotFound>> CreateCaseType(ICaseTypeService caseTypeService, CaseTypeRequest request) {
        if(request == null) {
            return TypedResults.NotFound();
        }
        await caseTypeService.Create(request);
        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<CaseType>, NotFound>> UpdateCaseType(ICaseTypeService caseTypeService, Guid caseTypeId, CaseTypeRequest request) {
        request.Id = caseTypeId; //TODO: This seems very wrong. Either the id is sent in the body or it's in the route. Not both.
        if (await caseTypeService.Update(request) is not { } caseTypeDetails) {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(caseTypeDetails);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCaseType(ICaseTypeService caseTypeService, Guid caseTypeId) {
        if (await caseTypeService.Get(caseTypeId) is not { }) {
            return TypedResults.NotFound();
        }
        await caseTypeService.Delete(caseTypeId);
        return TypedResults.NoContent();
    }
}
