using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class MyCasesHandlers
{
    public static async Task<Ok<ResultSet<CaseTypePartial>>> GetCaseTypes(
        IMyCaseService myCaseService,
        [AsParameters] ListOptions options, 
        string[]? caseTypeTags) {
        var results = await myCaseService.GetCaseTypes(ListOptions.Create(options, new GetMyCaseTypesListFilter { CaseTypeTags = [..caseTypeTags] }));
        return TypedResults.Ok(results);
    }
    public static async Task<Ok<CaseTypePartial>> GetCaseType(
        IMyCaseService myCaseService,
        string caseTypeCode)
    {
        var result = await myCaseService.GetCaseType(caseTypeCode);
        return TypedResults.Ok(result);
    }
}
