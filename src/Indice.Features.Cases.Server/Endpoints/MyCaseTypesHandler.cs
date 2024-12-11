using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Models.Responses;
using Indice.Features.Cases.Core.Services.Abstractions;
using Indice.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Indice.Features.Cases.Server.Endpoints;

internal static class MyCaseTypesHandler
{
    public static async Task<Ok<ResultSet<CaseTypePartial>>> GetCaseTypes(
        IMyCaseService myCaseService,
        [AsParameters] ListOptions options, [AsParameters] GetMyCaseTypesListFilter filter) =>
        TypedResults.Ok(await myCaseService.GetCaseTypes(ListOptions.Create(options, filter)));

    public static async Task<Ok<CaseTypePartial>> GetCaseType(
        IMyCaseService myCaseService,
        string caseTypeCode) =>
        TypedResults.Ok(await myCaseService.GetCaseType(caseTypeCode));
}
