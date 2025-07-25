using System.Security.Claims;
using Indice.Features.Cases.Core;
using Indice.Features.Cases.Core.Models;
using Indice.Features.Cases.Core.Services.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Indice.Features.Cases.Server.Endpoints;
internal static class AdminReportsHandlers
{
    public static async Task<Ok<List<GroupByReportResult>>> GetCaseReport(ReportTag reportTag, IAdminReportService adminReportService, 
        ClaimsPrincipal currentUser, 
        IOptions<CasesOptions> casesOptions) =>
        TypedResults.Ok(await adminReportService.GenerateReport(currentUser.UserToActor(casesOptions.Value), reportTag));
}
