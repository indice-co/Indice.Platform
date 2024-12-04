using System.Security.Claims;
using Indice.Features.Cases.Interfaces;
using Indice.Features.Cases.Models;
using Indice.Features.Cases.Models.Responses;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminCaseTypesHandler
{

    public static async Task<Results<Ok<ResultSet<CaseTypePartial>>, NotFound>> GetAdminCaseTypes(ICaseTypeService caseTypeService, ClaimsPrincipal User, [AsParameters] bool canCreate = false) {
        var caseTypes = await caseTypeService.Get(User, canCreate);
        if (caseTypes == null) {
               return TypedResults.NotFound();
        }
        return TypedResults.Ok(caseTypes);
    }

    public static async Task<Results<Ok<CaseType>, NotFound>> GetCaseTypeById(ICaseTypeService caseTypeService, Guid caseTypeId) {
        var caseTypeDetails = await caseTypeService.GetCaseTypeDetailsById(caseTypeId);
        if (caseTypeDetails == null) {
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
        request.Id = caseTypeId;
        var caseTypeDetails = await caseTypeService.Update(request);
        if (caseTypeDetails == null) {
               return TypedResults.NotFound();
        }
        return TypedResults.Ok(caseTypeDetails);
    }

    public static async Task<Results<NoContent, NotFound>> DeleteCaseType(ICaseTypeService caseTypeService, Guid caseTypeId) {
        var caseType = caseTypeService.Get(caseTypeId);
        if (caseType == null) {
               return TypedResults.NotFound();
        }
        await caseTypeService.Delete(caseTypeId);
        return TypedResults.NoContent();
    }
}
